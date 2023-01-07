using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using bsn.Har;

using Newtonsoft.Json;

namespace bsn.MFilesExtensionGateway {
	public static class HttpListenerExtensions {
		public static async ValueTask<HarRequest> ToHarRequest(this HttpListenerRequest httpRequest) {
			var harRequest = new HarRequest {
					Url = httpRequest.Url,
					Method = new HttpMethod(httpRequest.HttpMethod)
			};
			foreach (Cookie cookie in httpRequest.Cookies) {
				harRequest.Cookies.Add(cookie);
			}
			foreach (var requestHeader in httpRequest.Headers.AllKeys) {
				harRequest.Headers.Add(new HarNameValue() {
						Name = requestHeader,
						Value = httpRequest.Headers[requestHeader]
				});
			}
			if (httpRequest.ProtocolVersion != null) {
				harRequest.HttpVersion = $"HTTP/{httpRequest.ProtocolVersion.Major}.{httpRequest.ProtocolVersion.Minor}";
			}
			if (httpRequest.HasEntityBody) {
				var contentType = string.IsNullOrEmpty(httpRequest.ContentType) ? "application/octet-stream" : httpRequest.ContentType;
				if (HarExtensions.IsTextMimeType(contentType)) {
					using (var reader = new StreamReader(httpRequest.InputStream, httpRequest.ContentEncoding ?? Encoding.UTF8)) {
						harRequest.BodySize = (int)httpRequest.ContentLength64;
						harRequest.PostData = new HarRequest.HarPostData() {
								MimeType = contentType,
								Text = await reader.ReadToEndAsync().ConfigureAwait(false)
						};
					}
				} else {
					using (var buffer = new MemoryStream(Math.Max((int)httpRequest.ContentLength64, 4080))) {
						await httpRequest.InputStream.CopyToAsync(buffer).ConfigureAwait(false);
						harRequest.BodySize = (int)buffer.Length;
						harRequest.PostData = new HarRequest.HarPostData() {
								MimeType = contentType,
								Text = Convert.ToBase64String(buffer.GetBuffer(), 0, harRequest.BodySize, Base64FormattingOptions.None),
								Encoding = "base64"
						};
					}
				}
			}
			return harRequest;
		}

		public static HttpContent SerializeToHttpContent(this JsonSerializer serializer, object data) {
			var stream = new MemoryStream();
			using (var writer = new StreamWriter(stream, Encoding.UTF8, 4096, true)) {
				serializer.Serialize(writer, data);
			}
			stream.Seek(0, SeekOrigin.Begin);
			var content = new StreamContent(stream);
			content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			return content;
		}

		public static async ValueTask<T> DeserializeFromHttpContentAsync<T>(this JsonSerializer serializer, HttpContent content) {
			using var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
			using var textReader = new StreamReader(stream);
			using var jsonReader = new JsonTextReader(textReader);
			return serializer.Deserialize<T>(jsonReader);
		}

		public static async ValueTask SendHarResponseAsync(this HttpListenerResponse httpResponse, HarResponse harResponse, CancellationToken cancellationToken = default) {
			foreach (var header in harResponse.Headers) {
				if (!WebHeaderCollection.IsRestricted(header.Name, true)) {
					httpResponse.AppendHeader(header.Name, header.Value);
				}
			}
			foreach (var cookie in harResponse.Cookies) {
				httpResponse.AppendCookie(cookie);
			}
			if (harResponse.RedirectURL != null) {
				httpResponse.Redirect(harResponse.RedirectURL.ToString());
			} else {
				httpResponse.StatusCode = (int)harResponse.Status;
				httpResponse.StatusDescription = harResponse.StatusText ?? harResponse.Status.ToString();
			}
			if (harResponse.Content != null) {
				var data = harResponse.Content.ToByteArray();
				httpResponse.ContentType = harResponse.Content.MimeType;
				httpResponse.ContentLength64 = data.Length;
				await httpResponse.OutputStream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
			}
		}
	}
}
