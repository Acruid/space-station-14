using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.GameObjects.Exceptions
{
    class RequiredComponentMissingException : Exception
    {
        private readonly Type _type;

        public RequiredComponentMissingException(Type type)
        {
            _type = type;
        }

        public override string Message => $"The required component of type {_type} is missing from the entity.";
    }
}
