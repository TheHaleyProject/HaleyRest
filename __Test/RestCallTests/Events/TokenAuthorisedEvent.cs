﻿using Haley.Abstractions;
using Haley.Enums;
using Haley.Events;
using Haley.Models;
using Haley.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RestCallTests {
    public class TokenAuthorisedEvent : HEvent<(string verifier,string token)> {
    }
}
