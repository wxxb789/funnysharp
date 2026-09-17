using LanguageExt;

namespace CallSites.Competitors;

// W3 competitor: language-ext 4.4.9 Validation<Error, T> applicative accumulation.
public static class W3LanguageExt
{
    public static Validation<string, SignupForm> ValidateSignup(SignupForm form) =>
        (ValidateEmail(form.Email), ValidatePassword(form.Password), ValidateAge(form.Age))
            .Apply((email, password, age) => new SignupForm(email, password, age));

    private static Validation<string, string> ValidateEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains('@')
            ? Validation<string, string>.Success(email)
            : Validation<string, string>.Fail(Prelude.Seq<string>(["email must contain '@'"]));

    private static Validation<string, string> ValidatePassword(string password) =>
        password.Length >= 12
            ? Validation<string, string>.Success(password)
            : Validation<string, string>.Fail(Prelude.Seq<string>(["password must have at least 12 characters"]));

    private static Validation<string, int> ValidateAge(int age) =>
        age is >= 18 and <= 130
            ? Validation<string, int>.Success(age)
            : Validation<string, int>.Fail(Prelude.Seq<string>(["age must be between 18 and 130"]));
}
