using System;
using System.Net;
using System.Net.Sockets;
using BluetoothDevicePairing.Bluetooth;
using BluetoothDevicePairing.Command;
using BluetoothDevicePairing.Command.Utils;
using CommandLine;
using Windows.Devices.Enumeration;

namespace BluetoothDevicePairing
{
    internal sealed class Program
    {
        private static PairingServer sPairingServer = null;

        private static bool WaitOnError;

        private static bool IS_SOCK_MODE = true;

        private static void ParseCommandLineAndExecuteActions(string[] args)
        {
            Parser.Default.ParseArguments<PairDeviceOptions, DiscoverDevicesOptions, UnpairDeviceOptions>(args)
                .WithParsed<CommonOptions>(opts => WaitOnError = opts.WaitOnError)
                .WithParsed<PairDeviceOptions>(PairDevice.Execute)
                .WithParsed<UnpairDeviceOptions>(UnPairDevice.Execute)
                .WithParsed<DiscoverDevicesOptions>(DiscoverDevices.Execute);
        }

        private static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                Console.WriteLine(args[0]);
                if ("background".Equals(args[0]))
                {
                    IS_SOCK_MODE = true;
                }
                else if ("discover".Equals(args[0]))
                {
                    IS_SOCK_MODE = false;
                }
                else if ("pair".Equals(args[0]))
                {
                    IS_SOCK_MODE = false;
                }
            }

            try
            {
                if (IS_SOCK_MODE)
                {
                    sPairingServer = new PairingServer();
                    sPairingServer.NewMessageReceived += NewMessageReceived;
                    sPairingServer.Start();
                }
                else {
                    ParseCommandLineAndExecuteActions(args);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed: {ex.Message}");
                if (WaitOnError)
                {
                    Console.ReadLine();
                }

                return 1;
            }
        }

        private static void NewMessageReceived(PairingServer pairingServer, Socket socket, EndPoint epFrom, string newMessage)
        {
            if (string.IsNullOrEmpty(newMessage)) return;

            string[] parameters = newMessage.Split("/");

            string opCoode = parameters[0];

            if ("features".Equals(opCoode))
            {
                HandleFeaturesCommand(epFrom, parameters);
            }
            else if ("pair".Equals(opCoode))
            {
                HandlePairCommand(epFrom, parameters);
            }
        }

        private static void HandleFeaturesCommand(EndPoint epFrom, string[] parameters)
        {
            try
            {
                sPairingServer.SendTo(epFrom, 0x0001);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.StackTrace}");
            }
            return;
        }

        private static async void HandlePairCommand(EndPoint epFrom, string[] parameters)
        {
            if (parameters.Length < 3)
            {
                Console.WriteLine($"HandlePairCommand(): Too Short Parameters, length={parameters.Length}");
                return;
            }

            string name = parameters[1];
            string deviceId = parameters[2];

            Console.WriteLine($"HandlePairCommand(): Trying to pair with Name={name}, DeviceId={deviceId}");

            try
            {
                DeviceInformation devInfo = await DeviceInformation.CreateFromIdAsync(deviceId);
                Device device = new(devInfo);
                DevicePairer.PairDevice(device, "0000");

                sPairingServer.SendTo(epFrom, "OK");
            }
            catch (Exception ex)
            {
                sPairingServer.SendTo(epFrom, "FAIL");
                Console.WriteLine($"Exception: {ex.StackTrace}");                
            }
            return;
        }
    }
}
