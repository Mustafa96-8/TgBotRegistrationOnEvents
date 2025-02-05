namespace TelegramBot.Domain.Enums;

public static class Messages
{
    public static string SomethingWentWrong => "Что-то пошло не так. Попробуйте снова.";
    public static string PrintYouName => "Введите ваше имя:";
    public static string PrintPhoneNumber => "Введите ваш номер телефона.(Например: +7 987 654 32 10):";
    public static string ChangingPhoneNumber => "Номер телефона успешно изменён на";
    public static string WrongPhoneNumberFormat=>  "Неверный формат номера. (Например: +7 987 654 32 10):";
    public static string YouHaveRegisteredForTheEvent => "Вы зарегестрировались на мероприятие, спасибо";
    public static string AreYou18 => "Вам есть 18?";
    public static string RegistrationOnly18 => "Регистрация только для совершеннолетних пользователей";
    public static string GoRegistration => "Начните регистрацию /start:";
    public static string NewUserRegistered => "Новый пользователь зарегестрировался: ";
    public static string UserUnregrisered => "Пользователь отменил регистрацию: ";
}