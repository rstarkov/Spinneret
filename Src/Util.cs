using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace RT.Spinneret
{
    /// <summary>
    /// Utilities used by Spinneret, some of which are also intended for use by Spinneret users.
    /// </summary>
    public static class Util
    {
        public static T GetValidated<T>(this HttpRequest request, string varName)
        {
            return request.GetValidated<T>(varName, x => true, null);
        }

        public static T GetValidated<T>(this HttpRequest request, string varName, Func<T, bool> validator, string mustBe)
        {
            if (!request.Get.ContainsKey(varName))
                throw new ValidationException(varName, "<none>", "specified");

            T value;
            try { value = ExactConvert.To<T>(request.Get[varName][0]); }
            catch (ExactConvertException) { throw new ValidationException(varName, request.Get[varName][0], "convertible to {0}".Fmt(typeof(T))); }

            if (!validator(value))
                throw new ValidationException(varName, request.Get[varName][0], mustBe);

            return value;
        }

        public static T GetValidated<T>(this HttpRequest request, string varName, T varDefault)
        {
            return request.GetValidated<T>(varName, varDefault, x => true, null);
        }

        public static T GetValidated<T>(this HttpRequest request, string varName, T varDefault, Func<T, bool> validator, string mustBe)
        {
            if (!request.Get.ContainsKey(varName))
                return varDefault;

            T value;
            try { value = ExactConvert.To<T>(request.Get[varName][0]); }
            catch (ExactConvertException) { throw new ValidationException(varName, request.Get[varName][0], "convertible to {0}".Fmt(typeof(T))); }

            if (!validator(value))
                throw new ValidationException(varName, request.Get[varName][0], mustBe);

            return value;
        }

        public static string SameUrlExcept(this UrlPathRequest request, Dictionary<string, string> qsAddOrReplace, string[] qsRemove, string resturl)
        {
            StringBuilder sb = new StringBuilder(request.BaseUrl);
            if (resturl == null)
                sb.Append(request.UrlWithoutQuery);
            else
                sb.Append(resturl);
            char sep = '?';
            foreach (var kvp in request.Get)
            {
                if (qsRemove != null && qsRemove.Contains(kvp.Key))
                    continue;
                sb.Append(sep);
                sb.Append(kvp.Key.UrlEscape());
                sb.Append("=");
                if (qsAddOrReplace != null && qsAddOrReplace.ContainsKey(kvp.Key))
                {
                    sb.Append(qsAddOrReplace[kvp.Key].UrlEscape());
                    qsAddOrReplace.Remove(kvp.Key);
                }
                else
                {
                    sb.Append(kvp.Value[0].UrlEscape());
                }
                sep = '&';
            }
            if (qsAddOrReplace != null)
            {
                foreach (var kvp in qsAddOrReplace)
                {
                    sb.Append(sep);
                    sb.Append(kvp.Key.UrlEscape());
                    sb.Append("=");
                    sb.Append(kvp.Value.UrlEscape());
                    sep = '&';
                }
            }
            return sb.ToString();
        }

        public static string SameUrlExceptSet(this UrlPathRequest request, params string[] qsAddOrReplace)
        {
            var dict = new Dictionary<string, string>();
            if ((qsAddOrReplace.Length & 1) == 1)
                throw new RTException("Expected an even number of strings - one pair per query string argument");
            for (int i = 0; i < qsAddOrReplace.Length; i += 2)
                dict.Add(qsAddOrReplace[i], qsAddOrReplace[i + 1]);
            return request.SameUrlExcept(dict, null, null);
        }

        public static string SameUrlExceptRemove(this UrlPathRequest request, params string[] qsRemove)
        {
            return request.SameUrlExcept(null, qsRemove, null);
        }

        public static string SameUrlExceptSetRest(this UrlPathRequest request, string resturl)
        {
            return request.SameUrlExcept(null, null, resturl);
        }

        public static string SameUrlWhere(this UrlPathRequest request, Func<string, bool> predicate)
        {
            var qs = request.Get.Keys.Where(predicate).Select(key => request.Get[key].Select(val => key.UrlEscape() + "=" + val.UrlEscape()).JoinString("&")).JoinString("&");
            return request.OriginalUrlWithoutQuery + (qs == "" ? "" : "?" + qs);
        }
    }

    public class ValidationException : RTException
    {
        public ValidationException(string varName, string varValue, string mustBe)
        {
            _message = "The value of parameter \"{0}\", \"{1}\", is not valid. It must be {2}.".Fmt(varName, varValue, mustBe);
        }
    }

    public static class Throw
    {
        public static void IfArgumentNull(object arg, string argName)
        {
            if (arg == null)
                throw new ArgumentNullException(argName);
        }
    }
}
