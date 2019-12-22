using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace swtab
{
    class Config
    {
        readonly string SECTION_GAME_CONFIG = "GameConfig";
        readonly string SECTION_NAME = "Name";
        readonly string KEY_NAME_GAME_PATH = "GamePath";
        readonly string KEY_NAME_APP_LOCALE = "AppLocale";

        private INI ini;

        public Config(string fileName)
        {
            ini = new INI(fileName);
        }

        public string GamePath
        {
            get
            {
                return ini.ReadString(SECTION_GAME_CONFIG, KEY_NAME_GAME_PATH, "");
            }
            set
            {
                ini.WriteString(SECTION_GAME_CONFIG, KEY_NAME_GAME_PATH, value);
            }
        }

        public bool AppLocale
        {
            get
            {
                return ini.ReadInt(SECTION_GAME_CONFIG, KEY_NAME_APP_LOCALE, 1) == 1;
            }
            set
            {
                ini.WriteInt(SECTION_GAME_CONFIG, KEY_NAME_APP_LOCALE, value ? 1 : 0);
            }
        }

        public string GetName(string id)
        {
            return ini.ReadString(SECTION_NAME, id, id);
        }

        public void SetName(string id, string name)
        {
            ini.WriteString(SECTION_NAME, id, name);
        }
    }
}
