using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    // GET: api/Users
    [HttpGet]
    public ActionResult<IEnumerable<User>> GetUsers()
    {
        return Ok(_userService.GetAll());
    }

    // GET: api/Users/5
    [HttpGet("{id}")]
    public ActionResult<User> GetUser(int id)
    {
        var user = _userService.GetById(id);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    // POST: api/Users
    [HttpPost]
    public ActionResult<User> PostUser([FromBody] User user)
    {
        if (!ModelState.IsValid || UserExist(user.Id))
        {
            return BadRequest(ModelState);
        }

        _userService.Add(user);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    // PUT: api/Users/5
    [HttpPut("{id}")]
    public IActionResult PutUser(int id, [FromBody] User user)
    {
        if (id != user.Id || !UserExist(id))
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _userService.Update(user);
        return NoContent();
    }

    // DELETE: api/Users/5
    [HttpDelete("{id}")]
    public IActionResult DeleteUser(int id)
    {
        var user = _userService.GetById(id);
        if (user == null)
        {
            return NotFound();
        }

        _userService.Delete(id);
        return NoContent();
    }

    //true : User exist
    private bool UserExist(int id){
        return _userService.UserExist(id);
    }
}
