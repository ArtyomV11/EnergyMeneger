namespace EnergyMeneger.Models;

public static class Rules
{
    public static void Require(
        bool condition,
        string message,
        int statusCode = 400)
    {
        if (!condition)
            throw new ApiException(statusCode, message);
    }

    public static string Text(string? value, string field, int max = 200)
    {
        Require(!string.IsNullOrWhiteSpace(value), $"Заполните поле «{field}».");

        var result = value!.Trim();

        Require(result.Length <= max,
            $"Поле «{field}» не должно превышать {max} символов.");

        return result;
    }

    public static void Power(double value)
    {
        Require(double.IsFinite(value) && value >= 0,
            "Мощность должна быть конечным неотрицательным числом.");
    }

    public static void Limit(double value)
    {
        Power(value);
        Require(value > 0, "Лимит мощности должен быть больше нуля.");
    }

    public static void Range(double min, double max)
    {
        Power(min);
        Power(max);
        Require(min <= max, "Минимальная мощность превышает максимальную.");
    }

    public static void EnumValue<T>(T value) where T : struct, Enum
    {
        Require(Enum.IsDefined(typeof(T), value),
            "Недопустимое значение перечисления.");
    }

    public static DateTime Now()
    {
        var ticks = DateTime.UtcNow.Ticks;
        return new DateTime(ticks - ticks % 10, DateTimeKind.Utc);
    }
}
