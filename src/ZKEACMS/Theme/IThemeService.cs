/* http://www.zkea.net/ 
 * Copyright (c) ZKEASOFT. All rights reserved. 
 * http://www.zkea.net/licenses */

using Easy.RepositoryPattern;
using System.Collections.Generic;
namespace ZKEACMS.Theme
{
    public interface IThemeService : IService<ThemeEntity>
    {
        void SetPreview(string id);
        void CancelPreview();
        ThemeEntity GetCurrentTheme();
        void ChangeTheme(string id);
        //add by roc
        IEnumerable<ThemeEntity> GetAllThemes();
    }
}