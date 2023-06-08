using DiscriminatedUnionAnalyzer;

namespace TestConsole;

[DiscriminatedUnion]
public abstract class CreateUserResult
{
    private CreateUserResult()
    {
    }

    public sealed class Created : CreateUserResult
    {
        public Created(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }

    public sealed class EmailAlreadyRegistered : CreateUserResult
    {
    }

    public sealed class ParentNotFound : CreateUserResult
    {
    }
}
