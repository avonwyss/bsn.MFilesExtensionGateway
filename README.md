<!-- GRAPHIC -->

# bsn.MFilesExtensionGateway

Windows Service which acts as HTTP gateway to a M-Files Vault Extension Method, which can implement HTTP server functionality.

The gateway is based on HttpListener, so that it can co-exist on thre same port and host as IIS.

<!-- badges -->

---
## Links

- [M-Files Vault Extension Methods](https://developer.m-files.com/Built-In/VBScript/Vault-Extension-Methods/)
- [Exposing Vault Extension Methods in the VAF](https://developer.m-files.com/Frameworks/Vault-Application-Framework/Attributes/Vault-Extension-Methods/)
- [HAR 1.2 Spec](http://www.softwareishard.com/blog/har-12-spec/)

---
## Description

Generic HTTP server which serializes incoming requests to HAR, sends them to a M-Files Extension Method, and expects a HAR response to return to the HTTP client.

The M-Files extension method is then implemented like this:

```cs
[VaultExtensionMethod("HttpGateway", RequiredVaultAccess = MFVaultAccess.MFVaultAccessNone)]
public string MyVaultExtensionMethod(EventHandlerEnvironment env) {
    // Deserialize JSON string to HarRequest
	using var reader = new JsonTextReader(new StringReader(env.Input));
	var request = HarDocument.Serializer.Deserialize<HarRequest>(reader);
    
    // Any code here that takes a HarRequest and returns a HarResponse
	var response = dispatcher.Dispatch(request, (env, Configuration));

	// Fixup of location header
	var locationHeader = response.Headers.FirstOrDefault(h => string.Equals(h.Name, "Location", StringComparison.OrdinalIgnoreCase));
	if (locationHeader != null && locationHeader.Value.StartsWith("/")) {
		var prefixHeader = request.Headers.FirstOrDefault(h => string.Equals(h.Name, "X-Forwarded-Prefix", StringComparison.OrdinalIgnoreCase));
		if (prefixHeader != null) {
			locationHeader.Value = prefixHeader.Value+locationHeader.Value;
		}
	}

    // Send response to gateway
    using var writer = new StringWriter();
    HarDocument.Serializer.Serialize(writer, response);
    return writer.ToString();
}
```

## FAQ

- **Q**
  
  What was the motivation to do something like this? What is it good for?
  
  **A**
  
  The motivation was to be able to expose contents from M-Files dynamically on demand on the web. I have for instance implemented an ICS based calendar which gathers data from M-Files objects, and I'm thinking about implementing a CardDav server in order to share objects in M-Files as contacts. The advantage of this approach versus REST calls is that any type of request can be implemented (including web pages, binary files etc.), and that the M-Files credentials are unly by the gateway and they are thus not exposed. You could, of course, add a HTTP authentication layer yourself on top.

- **Q**
  
  Can I test the server on the command line?
  
  **A**
  
  Sure, just run the executable. Edit the configuration file first to connect to your vault.

- **Q**
  
  How can I install the service?
  
  **A**
  
  Use the `installutil.exe` from the framework. You can then change the service identity to use the "NT SERVICE\MFilesExtensionGateway" account.

- **Q**
  
  The server does not start (command line or service), why?
  
  **A**
  
  Most likely there is no HTTP.SYS reservation. When running on the console, using an elevated console should help. For the service, add a regisration like so:

  `netsh http add urlacl url=http://YOUR:80/URL user="NT SERVICE\MFilesExtensionGateway"`

- **Q**
  
  Can I use HTTPS?

  **A**
  
  Sure, but in addition to the HTTP.SYS reservation you need to assign a matching certificate:

  `add sslcert hostnameport=YOUR:443 certhash=0102030405060708090A appid={b7ee8a98-d4dd-42ed-a47e-11d114fe19bf} certstorename=MY`

---
## Source

[https://github.com/avonwyss/bsn.MFilesExtensionGateway](https://github.com/avonwyss/bsn.MFilesExtensionGateway)

---
## License

- **[MIT license](LICENSE.txt)**
- Copyright 2022 © Arsène von Wyss.
