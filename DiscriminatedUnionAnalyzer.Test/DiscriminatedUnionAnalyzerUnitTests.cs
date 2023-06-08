using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = DiscriminatedUnionAnalyzer.Test.CSharpCodeFixVerifier<
    DiscriminatedUnionAnalyzer.DiscriminatedUnionAnalyzerAnalyzer,
    DiscriminatedUnionAnalyzer.DiscriminatedUnionAnalyzerCodeFixProvider>;

namespace DiscriminatedUnionAnalyzer.Test
{
    [TestClass]
    public class DiscriminatedUnionAnalyzerUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class {|#0:TypeName|}
        {   
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";

            var expected = VerifyCS.Diagnostic("DiscriminatedUnionAnalyzer").WithLocation(0).WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
        
        [TestMethod]
        public async Task TestMethod3()
        {
            var test = """                
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ConsoleApplication1
{
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

    public class MyClass
    {
        public string MyMethod()
        {
            var result = GetResult();
            return result switch
            {
                CreateUserResult.Created created => "Created",
                CreateUserResult.EmailAlreadyRegistered => "EmailAlreadyRegistered",
                CreateUserResult.ParentNotFound => "ParentNotFound",
                _ => throw new SwitchExpressionException()
            };
        }

        private CreateUserResult GetResult()
        {
            return new CreateUserResult.EmailAlreadyRegistered();
        }
    }
}
""";

            await VerifyCS.VerifyAnalyzerAsync(test);
            //var expected = VerifyCS.Diagnostic("DiscriminatedUnionAnalyzer").WithLocation(0).WithArguments("TypeName");
            //await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
