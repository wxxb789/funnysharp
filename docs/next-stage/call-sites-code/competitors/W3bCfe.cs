using CSharpFunctionalExtensions;

namespace CallSites.Competitors;

// W3 alternative competitor: CFE 3.7.0 UnitResult accumulation via Result.Combine.
public static class W3bCfe
{
    public static Result<SignupForm, IReadOnlyList<string>> ValidateSignup(SignupForm form) =>
        Result.Combine(
                new[]
                {
                    ValidateEmail(form.Email),
                    ValidatePassword(form.Password),
                    ValidateAge(form.Age),
                },
                errors => (IReadOnlyList<string>)errors.SelectMany(list => list).ToArray())
            .Bind<SignupForm, IReadOnlyList<string>>(() => Result.Success<SignupForm, IReadOnlyList<string>>(form));

    private static UnitResult<IReadOnlyList<string>> ValidateEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains('@')
            ? UnitResult.Success<IReadOnlyList<string>>()
            : UnitResult.Failure<IReadOnlyList<string>>(["email must contain '@'"]);

    private static UnitResult<IReadOnlyList<string>> ValidatePassword(string password) =>
        password.Length >= 12
            ? UnitResult.Success<IReadOnlyList<string>>()
            : UnitResult.Failure<IReadOnlyList<string>>(["password must have at least 12 characters"]);

    private static UnitResult<IReadOnlyList<string>> ValidateAge(int age) =>
        age is >= 18 and <= 130
            ? UnitResult.Success<IReadOnlyList<string>>()
            : UnitResult.Failure<IReadOnlyList<string>>(["age must be between 18 and 130"]);
}
