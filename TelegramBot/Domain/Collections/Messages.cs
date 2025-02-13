using TelegramBot.Domain.Collections;

namespace TelegramBot.Domain.Enums;

public static class Messages
{

    public static string SomethingWentWrong => "Что-то пошло не так. Попробуйте снова.";
    public static string PrintYouName => "Введите ваше имя:";
    public static string PrintPhoneNumber => "Введите ваш номер телефона.(Например: +7 987 654 32 10):";
    public static string ChangingPhoneNumber => "Номер телефона успешно изменён на";
    public static string WrongPhoneNumberFormat=>  "Неверный формат номера. (Например: +7 987 654 32 10):";
    public static string YouHaveRegisteredForTheEvent => "Вы зарегестрировались на мероприятие: ";
    public static string YouHaveAlreadyRegistered => "Вы уже зарегестрированы";
    public static string AreYou18 => "Вам есть 18?";
    public static string RegistrationOnly18 => "Регистрация только для совершеннолетних пользователей";
    public static string GoRegistration => "Начните регистрацию /start:";
    public static string YouHasUnregistered => "Вы отменили регистрацию";
    public static string ErrorInRegistrOnEvent => "Ошибка во время регистрации на мероприятие";
    public static string ProfileWasDeleted => "Ваш профиль удалён";
    public static string Menu => "Меню";
    public static string YouHaveNoPermissionsToUseThisCommand => "У вас нет прав на использование этой команды ";


    public static class Event
    {
        public static string AllowedToRegistr => "Доступные для регистрации мероприятия";
        public static string EventWasDeleted => "Мероприятие удалено ";
        public static string EventNotFound => "Мероприятие не найдено";

        public static string YourEvents => "Ваши мероприятия";
    }

    public static class Admin
    {
        public static string Menu => "Меню администратора";
        public static string NewUserRegistered => "Новый пользователь зарегестрировался: ";
        public static string UserUnregrisered => "Пользователь отменил регистрацию: ";
        public static string PrintNewAdminId => "Введите Telegram id нового администратора:";
        public static string PrintDeletePersonId => "Введите Telegram id для удаления пользователя/Администратора:";
        public static string NewAdminAdded => "Новый администратор добавлен";
        public static string CountOfRegisteredUsersOnEvent => "Зарегестрировано на мероприятие:";
        public static string PrintEventName => "Введите название мероприятия:";
        public static string PrintEventDateTime => "Введите дату и время мероприятия (DD.MM.YYYY HH:MM):";
        public static string WrongDateTimeFormat => "Введите дату и время мероприятия (DD.MM.YYYY HH:MM):";
        public static string PrintEventDescription => "Введите описание мероприятия:";
        public static string EventsuccessfullyCreated => "Мероприятие успешно создано";
        public static string YouAlreadyOperatingWithEvent => "Вы уже работаете с другим мероприятием (если это не так сообщите о этой ошибке)";
        public static string EventNotFound => "Мероприятие не найдено в базе данных, обратитесь к разработчику";
        public static string YouWillReceiveNotifications => "Вы будете получать оповещения о новых пользователях";
        public static string YouWillNotReceiveNotifications => "Вы больше не будете получать оповещения о новых пользователях";
        public static string UsersNotFound => "Пользователей не найдено";
        public static string YouHaveBeenAssignedTheAdminRole => "Вам выдана роль администратора";
        public static string PersonWasDeleted => "Пользователь/Администратор удалён";
        public static string WrongTelegramId => "Неверный телеграм Id, Введите его снова";

    }

    public static class Contacts
    {
        public static string GetDeveloperContact => "Если возникли проблемы с ботом, пожалуйста обратитесь: " + ApplicationConstants.DeveloperContact;
        public static string GetSommelierContact => "Вы можете написать напрямую сомелье для регистрации на мероприятие: " + ApplicationConstants.AdministrationContact;
    }
}