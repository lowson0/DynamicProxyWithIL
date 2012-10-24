using System;

namespace DynamicProxy
{
	public class SecurityManager
	{
		public SecurityManager() {
		}

        public static bool IsMethodInRole( string userRole, string methodName ) {
            return true;
        }
    }
}
