using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TE.Plex
{
    public class EventSource
    {
        #region Event Delegates
        /// <summary>
        /// The delegate for the Message event handler.
        /// </summary>
        /// <param name="sender">
        /// The object that triggered the event.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        public delegate void MessageChangedEventHandler(object sender, string messagee);
        #endregion

        #region Events
        /// <summary>
        /// The MessageChanged event member.
        /// </summary>
        public event MessageChangedEventHandler MessageChanged;

        /// <summary>
        /// Triggered when the message has changed.
        /// </summary>
        protected virtual void OnMessageChanged(string message)
        {
            MessageChanged?.Invoke(this, message);
        }
        #endregion
    }
}
