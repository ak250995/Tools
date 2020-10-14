using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using Makaretu.Dns;

namespace LocalJaeger
{
    class Program
    {
        static ServiceDiscovery sd = new ServiceDiscovery();
        static void Main(string[] args)
        {
            string host = args.Length > 0 ? args[0] : ":12345";

            var hostAndPort = host.Split(":");
            var port = UInt16.Parse(hostAndPort[1]);
            if (File.Exists("./jaeger-all-in-one.exe"))
            {
                var prc = Process.Start("./jaeger-all-in-one.exe", $"--collector.zipkin.host-port {host}");
                Process.Start(new ProcessStartInfo {UseShellExecute = true, FileName = "http://localhost:16686/" });
            }
            else
            {
                Console.WriteLine("To be able to have real jaeger testing, download jaeger-all-in-one.exe and put in the same folder as the current exe file");
            }
            var hostIp = hostAndPort[0];
            var service = new ServiceProfile();
            var ips = string.IsNullOrEmpty(hostIp) ? null : new []
            {
                IPAddress.Parse(hostIp)
            };
            SetServiceProfile(service, Guid.NewGuid().ToString("N"), 
                "_ncr-isip-gateway._tcp", "jaeger.local", port, ips);

            sd.Advertise(service);
            
            Console.WriteLine("Press any key to stop");
            Console.ReadKey();
        }
        
        static void SetServiceProfile(ServiceProfile sp, string instanceName, string serviceName, 
            string hostName, ushort port, IPAddress[] addresses)
        {
            sp.InstanceName = instanceName;
            sp.ServiceName = serviceName;
            DomainName fullyQualifiedName = sp.FullyQualifiedName;
            sp.HostName = hostName;
            
            SRVRecord srvRecord = new SRVRecord();
            srvRecord.Name = fullyQualifiedName;
            srvRecord.Port = port;
            srvRecord.Target = sp.HostName;
            sp.Resources.Add(srvRecord);
            TXTRecord txtRecord = new TXTRecord();
            txtRecord.Name = fullyQualifiedName;
            txtRecord.Strings.Add("txtvers=1");
            sp.Resources.Add(txtRecord);
            foreach (IPAddress address in addresses ?? MulticastService.GetLinkLocalAddresses())
                sp.Resources.Add(AddressRecord.Create(sp.HostName, address));
        }

    }
}