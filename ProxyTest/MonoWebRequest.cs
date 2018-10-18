//
// MonoWebRequest.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Net;
using System.Threading;

namespace ProxyTest
{
	public class MonoWebRequest
	{
		private static volatile IWebProxy s_DefaultWebProxy;

		private static volatile bool s_DefaultWebProxyInitialized;

		private static object s_InternalSyncObject;

		private static object InternalSyncObject {
			get {
				if (s_InternalSyncObject == null) {
					object value = new object ();
					Interlocked.CompareExchange (ref s_InternalSyncObject, value, null);
				}
				return s_InternalSyncObject;
			}
		}

		public static IWebProxy DefaultWebProxy {
			get {
				return InternalDefaultWebProxy;
			}
			set {
				InternalDefaultWebProxy = value;
			}
		}

		internal static IWebProxy InternalDefaultWebProxy {
			get {
				if (!s_DefaultWebProxyInitialized) {
					lock (InternalSyncObject) {
						if (!s_DefaultWebProxyInitialized) {
							DefaultProxySectionInternal section = DefaultProxySectionInternal.GetSection ();
							if (section != null) {
								s_DefaultWebProxy = section.WebProxy;
							}
							s_DefaultWebProxyInitialized = true;
						}
					}
				}
				return s_DefaultWebProxy;
			}
			set {
				if (!s_DefaultWebProxyInitialized) {
					lock (InternalSyncObject) {
						s_DefaultWebProxy = value;
						s_DefaultWebProxyInitialized = true;
					}
				} else {
					s_DefaultWebProxy = value;
				}
			}
		}
	}
}
