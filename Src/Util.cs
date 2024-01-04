using RT.Serialization;
using RT.Servers;
using RT.Util.ExtensionMethods;

namespace RT.Spinneret;

/// <summary>Utilities used by Spinneret, some of which are also intended for use by Spinneret users.</summary>
public static class Util
{
    public static T GetValidated<T>(this HttpRequest request, string varName)
    {
        return request.GetValidated<T>(varName, x => true, null);
    }

    public static T GetValidated<T>(this HttpRequest request, string varName, Func<T, bool> validator, string mustBe)
    {
        var valueStr = request.Url[varName];
        if (valueStr == null)
            throw new ValidationException(varName, "<none>", "specified");

        T value;
        try { value = ExactConvert.To<T>(valueStr); }
        catch (ExactConvertException) { throw new ValidationException(varName, valueStr, "convertible to {0}".Fmt(typeof(T))); }

        if (!validator(value))
            throw new ValidationException(varName, valueStr, mustBe);

        return value;
    }

    public static T GetValidated<T>(this HttpRequest request, string varName, T varDefault)
    {
        return request.GetValidated<T>(varName, varDefault, x => true, null);
    }

    public static T GetValidated<T>(this HttpRequest request, string varName, T varDefault, Func<T, bool> validator, string mustBe)
    {
        var valueStr = request.Url[varName];
        if (valueStr == null)
            return varDefault;

        T value;
        try { value = ExactConvert.To<T>(valueStr); }
        catch (ExactConvertException) { throw new ValidationException(varName, valueStr, "convertible to {0}".Fmt(typeof(T))); }

        if (!validator(value))
            throw new ValidationException(varName, valueStr, mustBe);

        return value;
    }
}

public class ValidationException : Exception
{
    public ValidationException(string varName, string varValue, string mustBe)
        : base("The value of parameter \"{0}\", \"{1}\", is not valid. It must be {2}.".Fmt(varName, varValue, mustBe))
    {
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
