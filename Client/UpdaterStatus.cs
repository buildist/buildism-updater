using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Updater
{
    class UpdaterStatus
    {
        public int status;
        public float progress;
        public string message;
        public UpdaterStatus(int status, float progress, string message)
        {
            this.status = status;
            this.progress = progress;
            this.message = message;
        }
    }
}
