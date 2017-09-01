using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using Scree.Common;
using Scree.Core.IoC;
using Scree.DataBase;
using Scree.Log;
using Scree.Persister;

namespace MyUI.Models
{
    public static class NewsService
    {
        public static IPersisterService PersisterService
        {
            get
            {
                return ServiceRoot.GetService<IPersisterService>();
            }
        }

        public static AddNewsOutDTO AddNews(AddNewsInDTO inDto)
        {
            AddNewsOutDTO outDto = new AddNewsOutDTO();
            try
            {
                if (inDto == null || string.IsNullOrEmpty(inDto.Title))
                {
                    outDto.ErrorMsg = "参数错误";
                    return outDto;
                }

                if (inDto.Title.Length > 50)
                {
                    outDto.ErrorMsg = "标题长度错误";
                    return outDto;
                }
                //如果直接new也是可以的，目前是等效的，建议统一使用CreateObject，未来可以利用CreateObject搞一些事情
                //News news = new News();
                News news = PersisterService.CreateObject<News>();
                news.SetId(TimeStampService.GetOneId<News>());

                news.Title = inDto.Title;
                news.Context = inDto.Context;
                news.Author = inDto.Author;
                news.Type = inDto.Type;

                string remark = "增加新闻";
                SystemLog systemLog = LogService.CreateSystemLog(SystemLogType.AddNews, typeof(News), news.Id, remark);

                PersisterService.SaveObject(new SRO[] { news, systemLog });
                outDto.IsSucceed = true;
                outDto.Id = news.Id;
            }
            catch (Exception ex)
            {
                outDto.ErrorMsg = ex.Message;
                LogProxy.Error(ex, false);
            }

            return outDto;
        }

        public static AlterNewsOutDTO AlterNews(AlterNewsInDTO inDto)
        {
            AlterNewsOutDTO outDto = new AlterNewsOutDTO();
            try
            {
                if (inDto == null || string.IsNullOrEmpty(inDto.Title))
                {
                    outDto.ErrorMsg = "参数错误";
                    return outDto;
                }

                if (inDto.Title.Length > 50)
                {
                    outDto.ErrorMsg = "标题长度错误";
                    return outDto;
                }

                News news = GetNews(inDto.Id, LoadType.DataBaseDirect);
                if (news == null)
                {
                    outDto.ErrorMsg = "新闻不存在";
                    return outDto;
                }

                news.Title = inDto.Title;
                news.Context = inDto.Context;
                news.Author = inDto.Author;
                news.Type = inDto.Type;

                string remark = "修改新闻";
                SystemLog systemLog = LogService.CreateSystemLog(SystemLogType.AlterNews, typeof(News), news.Id, remark);

                PersisterService.SaveObject(new SRO[] { news, systemLog });
                outDto.IsSucceed = true;
            }
            catch (Exception ex)
            {
                outDto.ErrorMsg = ex.Message;
                LogProxy.Error(ex, false);
            }

            return outDto;
        }

        public static DeleteNewsOutDTO DeleteNews(DeleteNewsInDTO inDto)
        {
            DeleteNewsOutDTO outDto = new DeleteNewsOutDTO();
            try
            {
                if (inDto == null || string.IsNullOrEmpty(inDto.Id))
                {
                    outDto.ErrorMsg = "参数错误";
                    return outDto;
                }

                News news = NewsService.GetNews(inDto.Id, LoadType.DataBaseDirect);
                if (news == null)
                {
                    outDto.ErrorMsg = "新闻不存在";
                    return outDto;
                }

                news.IsDeleted = true;

                string remark = "删除新闻";
                SystemLog systemLog = LogService.CreateSystemLog(SystemLogType.DeleteNews, typeof(News), news.Id, remark);

                PersisterService.SaveObject(new SRO[] { news, systemLog });
                outDto.IsSucceed = true;
            }
            catch (Exception ex)
            {
                outDto.ErrorMsg = ex.Message;
                LogProxy.Error(ex, false);
            }

            return outDto;
        }

        public static ReadingNewsOutDTO ReadingNews(DeleteNewsInDTO inDto)
        {
            ReadingNewsOutDTO outDto = new ReadingNewsOutDTO();
            try
            {
                if (inDto == null || string.IsNullOrEmpty(inDto.Id))
                {
                    outDto.ErrorMsg = "参数错误";
                    return outDto;
                }

                News news = NewsService.GetNews(inDto.Id, LoadType.DataBaseDirect);
                if (news == null)
                {
                    outDto.ErrorMsg = "新闻不存在";
                    return outDto;
                }

                news.ReadingQuantity++;

                PersisterService.SaveObject(news);
                outDto.IsSucceed = true;
                outDto.ReadingQuantity = news.ReadingQuantity;
            }
            catch (Exception ex)
            {
                outDto.ErrorMsg = ex.Message;
                LogProxy.Error(ex, false);
            }

            return outDto;
        }

        public static GetNewsOutDTO GetNews(GetNewsInDTO inDto)
        {
            GetNewsOutDTO outDto = new GetNewsOutDTO();
            try
            {
                if (inDto == null || string.IsNullOrEmpty(inDto.Id))
                {
                    outDto.ErrorMsg = "参数错误";
                    return outDto;
                }

                News news = GetNews(inDto.Id, LoadType.CacheFirst);

                if (news == null)
                {
                    outDto.ErrorMsg = "新闻不存在";
                    return outDto;
                }

                outDto.IsSucceed = true;

                outDto.Id = news.Id;
                outDto.Title = news.Title;
                outDto.Context = news.Context;
                outDto.Author = news.Author;
                outDto.Type = news.Type;
                outDto.CreatedDate = news.CreatedDate;
                outDto.LastAlterDate = news.LastAlterDate;
            }
            catch (Exception ex)
            {
                outDto.ErrorMsg = ex.Message;
                LogProxy.Error(ex, false);
            }

            return outDto;
        }

        internal static News GetNews(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            News obj = PersisterService.LoadObject<News>(id);

            return obj;
        }
        internal static News GetNews(string id, LoadType loadType)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            News obj = PersisterService.LoadObject<News>(id, loadType);

            return obj;
        }
        internal static News[] GetNewsByType(NewsType type, LoadType loadType)
        {
            List<IMyDbParameter> prams = new List<IMyDbParameter>();
            prams.Add(DbParameterProxy.Create("Type", SqlDbType.Int, (int)type));

            News[] objs = PersisterService.LoadObjects<News>("[Type]=@Type", prams.ToArray(), loadType);

            return objs;
        }
        internal static News[] GetNewsByAuthor(string author, LoadType loadType)
        {
            List<IMyDbParameter> prams = new List<IMyDbParameter>();
            prams.Add(DbParameterProxy.Create("Author", SqlDbType.NVarChar, "%" + author + "%"));

            News[] objs = PersisterService.LoadObjects<News>("[Author] like @Author order by ReadingQuantity desc", prams.ToArray(), loadType);

            return objs;
        }
    }
}
