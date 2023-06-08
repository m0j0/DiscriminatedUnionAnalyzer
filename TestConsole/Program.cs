using System.Runtime.CompilerServices;
using TestConsole;

var result = GetResult();
var str = result switch
{
    CreateUserResult.Created created => "Created" + created.Id,
    CreateUserResult.EmailAlreadyRegistered => "EmailAlreadyRegistered", 
    CreateUserResult.ParentNotFound => "ParentNotFound",
    _ => throw new SwitchExpressionException()
};

Console.WriteLine(str);

static CreateUserResult GetResult()
{
    return new CreateUserResult.EmailAlreadyRegistered();
}