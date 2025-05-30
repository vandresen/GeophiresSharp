﻿using GeophiresLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeophiresLibrary.Services
{
    public interface IModeling
    {
        string Modeling(InputParameters input);
        string GetCalculatedJsonResult();
    }
}
