using System;
using System.Configuration;
using System.Threading;
using System.Net;
using System.Net.Configuration;

namespace ProxyTest
{
	internal sealed class DefaultProxySectionInternal
	{
		private IWebProxy webProxy;

		private static object classSyncObject;

		internal static object ClassSyncObject {
			get {
				if (classSyncObject == null) {
					object value = new object ();
					Interlocked.CompareExchange (ref classSyncObject, value, null);
				}
				return classSyncObject;
			}
		}

		internal IWebProxy WebProxy => webProxy;

		private static IWebProxy GetDefaultProxy_UsingOldMonoCode ()
		{
			DefaultProxySection defaultProxySection = ConfigurationManager.GetSection ("system.net/defaultProxy") as DefaultProxySection;
			if (defaultProxySection == null) {
				Console.WriteLine ("DefaultProxySectionInternal: defaultProxySection is null returning GetSystemWebProxy()");
				return GetSystemWebProxy ();
			}
			ProxyElement proxy = defaultProxySection.Proxy;
			WebProxy webProxy;
			if (proxy.UseSystemDefault != 0 && proxy.ProxyAddress == (Uri)null) {
				Console.WriteLine ("DefaultProxySectionInternal: defaultProxySection not being used");
				IWebProxy systemWebProxy = GetSystemWebProxy ();
				if (!(systemWebProxy is WebProxy)) {
					Console.WriteLine ("DefaultProxySectionInternal: return  GetSystemWebProxy(). system proxy type: {0}", systemWebProxy.GetType().FullName);
					return systemWebProxy;
				}
				webProxy = (WebProxy)systemWebProxy;
			} else {
				webProxy = new WebProxy ();
			}
			if (proxy.ProxyAddress != (Uri)null) {
				Console.WriteLine ("DefaultProxySectionInternal: setting proxyAddress {0}", proxy.ProxyAddress);
				webProxy.Address = proxy.ProxyAddress;
			}
			if (proxy.BypassOnLocal != ProxyElement.BypassOnLocalValues.Unspecified) {
				webProxy.BypassProxyOnLocal = (proxy.BypassOnLocal == ProxyElement.BypassOnLocalValues.True);
			}
			foreach (BypassElement bypass in defaultProxySection.BypassList) {
				webProxy.BypassArrayList.Add (bypass.Address);
			}
			return webProxy;
		}

		private static IWebProxy GetSystemWebProxy ()
		{
			return MonoWebProxy.CreateDefaultProxy ();
		}

		internal static DefaultProxySectionInternal GetSection ()
		{
			lock (ClassSyncObject) {
				return new DefaultProxySectionInternal {
					webProxy = GetDefaultProxy_UsingOldMonoCode ()
				};
			}
		}
	}
}
