using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace bsn.MFilesExtensionGateway {
	internal static class Program {
		private static void Main(string[] args) {
			if (!HttpListener.IsSupported) {
				throw new InvalidOperationException("HttpListener support required.");
			}
			var service = new MFilesExtensionGatewayService();
			if (Environment.UserInteractive) {
				service.RunAsConsole(args);
			} else {
				ServiceBase.Run(new ServiceBase[] {
						service
				});
			}
		}
	}
}
