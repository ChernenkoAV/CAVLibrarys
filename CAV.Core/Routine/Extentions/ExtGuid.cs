﻿using System;

namespace Cav
{
    /// <summary>
    /// Расширения для Guid
    /// </summary>
    public static class ExtGuid
    {
        /// <summary>
        /// Получение короткой строки Guid
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static String ToShortString(this Guid guid)
        {
            return Convert.ToBase64String(guid.ToByteArray()).TrimEnd('=').Replace('/', '_').Replace('+', '-');
        }

        /// <summary>
        /// Получение Guid из короткой строки
        /// </summary>
        /// <param name="strGuid"></param>
        /// <returns>null, if string null</returns>
        public static Guid? GuidFromShortString(this String strGuid)
        {
            Guid? res = null;

            if (strGuid.IsNullOrWhiteSpace())
                return res;

            strGuid = strGuid.Replace('_', '/').Replace('-', '+') + "====";

            return new Guid(Convert.FromBase64String(strGuid));
        }
    }
}