using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyLinq
{
    public enum CursorState
    {
        /// <summary>
        /// The cursor needs to initialise its enumerator.
        /// </summary>
        Uninitialised = 0,

        /// <summary>
        /// The cursor is active.
        /// </summary>
        Active,

        /// <summary>
        /// The cursor's enumerator has run out, or had nothing to begin with.
        /// </summary>
        Exhausted,
    }
}
