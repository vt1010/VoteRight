using System;

namespace VoteRightWebApp.Services
{
    public class BoothsNotFoundException : Exception
    {
        public BoothsNotFoundException() { }
        public BoothsNotFoundException(string message) : base(message) { }
        public BoothsNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}