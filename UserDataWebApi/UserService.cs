

public class UserService
{
    private readonly List<User> _users = new List<User>();
    public IEnumerable<User> GetAll() => _users;

    public bool UserExist(int id) => GetById(id) != null;


    public UserService(){
        _users.Add(new User
        {
            Id = 1,
            Name = "John",
            Email = "john@example.com"
        });

        _users.Add(new User
        {
            Id = 2,
            Name = "Jane",
            Email = "jane@example.com"
        });

        _users.Add(new User
        {
            Id = 3,
            Name = "Alice",
            Email = "alice@example.com"
        });

        _users.Add(new User
        {
            Id = 4,
            Name = "Bob",
            Email = "bob@example.com"
        });
    }

    public User GetById(int id) => _users.FirstOrDefault(u => u.Id == id);

    public void Add(User user)
    {
        user.Id = _users.Count > 0 ? _users.Max(u => u.Id) + 1 : 1;
        _users.Add(user);
    }

    public void Update(User user)
    {
        var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
        if (existingUser != null)
        {
            existingUser.Name = user.Name;
            existingUser.Email = user.Email;
        }
    }

    public void Delete(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user != null)
        {
            _users.Remove(user);
        }
    }


}
