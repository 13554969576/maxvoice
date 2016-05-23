using Microsoft.VisualStudio.TestTools.UnitTesting;
using Maxvoice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maxvoice.Tests
{
    [TestClass()]
    public class ProgramTests
    {
        [TestMethod()]
        public void testTest()
        {
            new Program().test();
            //Assert.Fail();
        }
    }
}