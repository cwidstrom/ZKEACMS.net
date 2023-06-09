/* http://www.zkea.net/ 
 * Copyright (c) ZKEASOFT. All rights reserved. 
 * http://www.zkea.net/licenses */

using Easy.Cache;
using Easy.Extend;
using Easy.RepositoryPattern;
using FastExpressionCompiler;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Easy.Modules.MutiLanguage
{
    public class LanguageService : ILanguageService
    {
        private readonly ICacheManager<ConcurrentDictionary<string, ConcurrentDictionary<string, LanguageEntity>>> _cacheManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public LanguageService(ICacheManager<ConcurrentDictionary<string, ConcurrentDictionary<string, LanguageEntity>>> cacheManager, IWebHostEnvironment webHostEnvironment)
        {
            _cacheManager = cacheManager;
            _webHostEnvironment = webHostEnvironment;
        }

        private string GetLocaleDirectory()
        {
            return Path.Combine(_webHostEnvironment.ContentRootPath, "Locale");
        }

        private IEnumerable<LanguageEntity> GetAllFromLocaleFile()
        {
            List<LanguageEntity> entities = new List<LanguageEntity>();
            string localeDir = GetLocaleDirectory();
            string[] localeFiles = Directory.GetFiles(localeDir, "*.yml");
            foreach (string localeFile in localeFiles)
            {
                string cultureName = Path.GetFileNameWithoutExtension(localeFile);
                Dictionary<string, string> localize = ReadLocalizeTextFromFile(localeFile);
                foreach (var item in localize)
                {
                    entities.Add(new LanguageEntity
                    {
                        CultureName = cultureName,
                        LanKey = item.Key,
                        LanValue = item.Value
                    });
                }
            }
            return entities;
        }

        private static Dictionary<string, string> ReadLocalizeTextFromFile(string localeFile)
        {
            string localizeText = File.ReadAllText(localeFile, Encoding.UTF8);
            var deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
            var localize = deserializer.Deserialize<Dictionary<string, string>>(localizeText);
            return localize;
        }

        private static void SaveLocalizeTextToFile(string localeFile, Dictionary<string, string> localizeText)
        {
            var serializer = new SerializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
            string serializedData = serializer.Serialize(localizeText);
            File.WriteAllText(localeFile, serializedData, Encoding.UTF8);
        }

        private ConcurrentDictionary<string, ConcurrentDictionary<string, LanguageEntity>> GetAllFromCache()
        {
            return _cacheManager.GetOrAdd("AllLanguageEntry", factory =>
            {
                ExpireCacheOnFileChanged();

                ConcurrentDictionary<string, ConcurrentDictionary<string, LanguageEntity>> result = new ConcurrentDictionary<string, ConcurrentDictionary<string, LanguageEntity>>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in GetAllFromLocaleFile())
                {
                    if (!result.TryGetValue(item.LanKey, out ConcurrentDictionary<string, LanguageEntity> cultureDic))
                    {
                        cultureDic = new ConcurrentDictionary<string, LanguageEntity>(StringComparer.OrdinalIgnoreCase);
                        cultureDic.TryAdd(item.CultureName, item);
                        result.TryAdd(item.LanKey, cultureDic);
                    }
                    else
                    {
                        if (!cultureDic.ContainsKey(item.CultureName))
                        {
                            cultureDic.TryAdd(item.CultureName, item);
                        }
                    }

                }
                return result;
            });
        }

        private void ExpireCacheOnFileChanged()
        {
            if (!_webHostEnvironment.IsDevelopment()) return;

            _webHostEnvironment.ContentRootFileProvider.Watch("Locale/*.yml").RegisterChangeCallback(cm =>
            {
                (cm as ICacheManager<ConcurrentDictionary<string, ConcurrentDictionary<string, LanguageEntity>>>).Clear();
            }, _cacheManager);
        }

        private ServiceResult<LanguageEntity> Save(LanguageEntity item)
        {
            var result = new ServiceResult<LanguageEntity>();
            var localeFile = Path.Combine(GetLocaleDirectory(), $"{item.CultureName}.yml");
            try
            {
                var localizeText = ReadLocalizeTextFromFile(localeFile);
                localizeText[item.LanKey] = item.LanValue;
                SaveLocalizeTextToFile(localeFile, localizeText);
                _cacheManager.Clear();
            }
            catch (Exception ex)
            {
                result.AddRuleViolation(ex.Message);
            }
            return result;
        }

        public ServiceResult<LanguageEntity> Add(LanguageEntity item)
        {
            return Save(item);
        }
        public LanguageEntity Get(string lanKey, string cultureName)
        {
            LanguageEntity languageEntity = null;
            if (GetAllFromCache().TryGetValue(lanKey, out ConcurrentDictionary<string, LanguageEntity> cultureLan))
            {
                cultureLan.TryGetValue(cultureName, out languageEntity);
            }
            return languageEntity;
        }

        public IEnumerable<LanguageEntity> GetCultures(string lanKey)
        {
            if (GetAllFromCache().TryGetValue(lanKey, out ConcurrentDictionary<string, LanguageEntity> cultureDic))
            {
                foreach (var item in cultureDic)
                {
                    yield return item.Value;
                }
            }
        }
        public ServiceResult<LanguageEntity> Update(LanguageEntity item)
        {
            return Save(item);
        }


        public ServiceResult<LanguageEntity> AddOrUpdate(LanguageEntity entity)
        {
            LanguageEntity translate = Get(entity.LanKey, entity.CultureName);
            if (translate == null)
            {
                return Add(entity);
            }
            else
            {
                translate.LanValue = entity.LanValue;
                return Update(translate);
            }
        }

        public IEnumerable<LanguageEntity> Get(Expression<Func<LanguageEntity, bool>> expression, Pagination pagination)
        {
            var allLanguage = GetAllFromLocaleFile();

            if (pagination.OrderBy.IsNotNullAndWhiteSpace())
            {
                allLanguage = allLanguage.OrderBy(GetPropertyDelegate(pagination.OrderBy));
            }
            else if (pagination.OrderByDescending.IsNotNullAndWhiteSpace())
            {
                allLanguage = allLanguage.OrderByDescending(GetPropertyDelegate(pagination.OrderByDescending));
            }

            if (pagination.ThenBy.IsNotNullAndWhiteSpace())
            {
                allLanguage = (allLanguage as IOrderedEnumerable<LanguageEntity>).ThenBy(GetPropertyDelegate(pagination.ThenBy));
            }
            else if (pagination.ThenByDescending.IsNotNullAndWhiteSpace())
            {
                allLanguage = (allLanguage as IOrderedEnumerable<LanguageEntity>).ThenByDescending(GetPropertyDelegate(pagination.ThenByDescending));
            }
            if (expression != null)
            {
                allLanguage = allLanguage.Where(expression.CompileFast()).ToList();
            }
            pagination.RecordCount = allLanguage.Count();
            return allLanguage.Skip(pagination.PageIndex * pagination.PageSize).Take(pagination.PageSize);
        }

        private Func<LanguageEntity, object> GetPropertyDelegate(string propertyName)
        {
            Type type = typeof(LanguageEntity);

            var objPara = Expression.Parameter(type, "m");
            Expression expProperty = Expression.Property(objPara, propertyName);
            Expression propertyToObject = Expression.Convert(expProperty, typeof(object));
            var propertyDelegateExpression = Expression.Lambda(propertyToObject, objPara);
            return (Func<LanguageEntity, object>)propertyDelegateExpression.CompileFast();
        }
    }
}
