using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAdapterTest.Mocks
{
    class MockMessageLogger : IMessageLogger
    {
        public void SendMessage( TestMessageLevel testMessageLevel, string message )
        {
            // Do nothing.
        }
    }
}
