using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AutomationUtilities.Models;

namespace BotAutomationService
{
    internal interface IDataAccess
    {
        public Notice GetNotice();
        public void SaveNotice();
    }
}
