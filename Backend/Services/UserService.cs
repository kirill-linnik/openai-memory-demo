using Backend.Models;

namespace Backend.Services;

public class UserService
{
    private static readonly User _user = new()
    {
        Name = "Kirill Linnik",
    };

    public User GetUser()
    {
        return _user;
    }
}
