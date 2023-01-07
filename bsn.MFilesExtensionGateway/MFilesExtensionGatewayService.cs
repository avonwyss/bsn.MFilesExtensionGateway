using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using bsn.Har;
using bsn.MFilesExtensionGateway.Configuration;

namespace bsn.MFilesExtensionGateway {
	public partial class MFilesExtensionGatewayService: ServiceBase {
		private static readonly Regex rxHeaderRemove = new Regex(@"^(Connection|Keep-Alive|Close|Transfer-Encoding|Upgrade|Proxy-.+|Alt-Svc|TraceParent|Request-Id|TraceState|Baggage|Correlation-Context|HTTP2-Settings|ALPN|Security-Scheme|X-Original-URL|X-Rewrite-URL|X-Forwarded-.+)$",
				RegexOptions.Compiled|RegexOptions.CultureInvariant|RegexOptions.IgnoreCase|RegexOptions.ExplicitCapture|RegexOptions.Singleline);

		private readonly MFilesExtensionGatewaySection configuration;
		private HttpListener listener;
		private HttpClient client;
		private CancellationTokenSource cancellationSource;

		public MFilesExtensionGatewayService() {
			InitializeComponent();
			configuration = (MFilesExtensionGatewaySection)ConfigurationManager.GetSection("gateway");
		}

		public void RunAsConsole(string[] args) {
			OnStart(args);
			Console.WriteLine("Press any key to exit...");
			Console.ReadKey(true);
			OnStop();
		}

		protected override void OnStart(string[] args) {
			cancellationSource = new CancellationTokenSource();
			client = new HttpClient();
			client.BaseAddress = new Uri(configuration.Prefix.MFilesWeb);
			client.DefaultRequestHeaders.Add("X-Vault", configuration.Prefix.Vault.ToString("B"));
			client.DefaultRequestHeaders.Add("X-Username", configuration.Prefix.Username);
			client.DefaultRequestHeaders.Add("X-Password", configuration.Prefix.Password);
			listener = new HttpListener();
			listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
			listener.Prefixes.Add(configuration.Prefix.Url);
			listener.Start();
			listener.BeginGetContext(HandleContext, null);
		}

		private async void HandleContext(IAsyncResult ar) {
			try {
				var context = listener.EndGetContext(ar);
				try {
					cancellationSource.Token.ThrowIfCancellationRequested();
					listener.BeginGetContext(HandleContext, null);
					var harRequest = await context.Request.ToHarRequest().ConfigureAwait(false);
					harRequest.Headers.RemoveAll(h => rxHeaderRemove.IsMatch(h.Name) || (string.Equals(h.Name, "TE", StringComparison.OrdinalIgnoreCase) && !string.Equals(h.Value, "trailers", StringComparison.OrdinalIgnoreCase)));
					harRequest.Headers.Add(new HarNameValue() {
							Name = "X-Forwarded-URL",
							Value = context.Request.RawUrl
					});
					harRequest.Headers.Add(new HarNameValue() {
							Name = "X-Forwarded-Proto",
							Value = context.Request.Url.Scheme
					});
					harRequest.Headers.Add(new HarNameValue() {
							Name = "X-Forwarded-Host",
							Value = context.Request.Url.IsDefaultPort ? context.Request.Url.Host : FormattableString.Invariant($"{context.Request.Url.Host}:{context.Request.Url.Port}")
					});
					harRequest.Headers.Add(new HarNameValue() {
							Name = "X-Forwarded-Prefix",
							Value = context.Request.Url.AbsolutePath.Substring(0, configuration.Prefix.PathLength)
					});
					if (context.Request.RemoteEndPoint != null) {
						harRequest.Headers.Add(new HarNameValue() {
								Name = "X-Forwarded-For",
								Value = context.Request.RemoteEndPoint.Address.ToString()
						});
					}
					var builder = new UriBuilder(harRequest.Url);
					builder.Path = builder.Path.Substring(configuration.Prefix.PathLength);
					harRequest.Url = builder.Uri;
					using var response = await client.PostAsync(new Uri(client.BaseAddress, "/REST/vault/extensionmethod/HttpGateway"), HarDocument.Serializer.SerializeToHttpContent(harRequest)).ConfigureAwait(false);
					response.EnsureSuccessStatusCode();
					var harResponse = await HarDocument.Serializer.DeserializeFromHttpContentAsync<HarResponse>(response.Content).ConfigureAwait(false);
					await context.Response.SendHarResponseAsync(harResponse, cancellationSource.Token).ConfigureAwait(false);
				} catch (Exception ex) {
					var errorBytes = Encoding.UTF8.GetBytes($@"<html>
	<head>
		<title>Internal Server Error</title>
	</head>
	<body>
		<h1>Internal Server Error</h1>
		<pre>{WebUtility.HtmlEncode(ex.ToString())}</pre>
	</body>
</html>");
					context.Response.StatusCode = 500;
					context.Response.StatusDescription = "Internal Server Error";
					context.Response.Headers.Clear();
					context.Response.ContentType = "text/html; encoding=utf-8";
					context.Response.ContentLength64 = errorBytes.Length;
					await context.Response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length).ConfigureAwait(false);
				} finally {
					context.Response.Close();
				}
			} catch (ObjectDisposedException) { }
		}

		protected override void OnStop() {
			cancellationSource.Cancel();
			listener.Stop();
			listener.Close();
			client.Dispose();
			cancellationSource.Dispose();
		}
	}
}
