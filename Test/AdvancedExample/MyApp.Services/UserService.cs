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

namespace MyApp.Models
{
    public static class UserService
    {
        public static IPersisterService PersisterService
        {
            get
            {
                return ServiceRoot.GetService<IPersisterService>();
            }
        }

        public static AddUserOutDTO AddUser(AddUserInDTO inDto)
        {
            AddUserOutDTO outDto = new AddUserOutDTO();
            try
            {
                if (inDto == null || string.IsNullOrEmpty(inDto.Name))
                {
                    outDto.ErrorMsg = "参数错误";
                    return outDto;
                }

                //如果直接new也是可以的，目前是等效的，建议统一使用CreateObject，未来可以利用CreateObject搞一些事情
                //User User = new User();
                User user = PersisterService.CreateObject<User>();
                user.SetId(TimeStampService.GetOneId<User>(inDto.Gender.ToString()));

                user.Name = inDto.Name;
                user.Gender = inDto.Gender;
                user.Mobile = inDto.Mobile;
                user.AvailableBalance = inDto.AvailableBalance;

                string remark = "增加用户";
                SystemLog systemLog = LogService.CreateSystemLog(SystemLogType.AddUser, typeof(User), user.Id, remark);

                PersisterService.SaveObject(new SRO[] { user, systemLog });
                outDto.IsSucceed = true;
                outDto.Id = user.Id;
            }
            catch (Exception ex)
            {
                outDto.ErrorMsg = ex.Message;
                LogProxy.Error(ex, false);
            }

            return outDto;
        }

        public static AlterUserOutDTO AlterUser(AlterUserInDTO inDto)
        {
            AlterUserOutDTO outDto = new AlterUserOutDTO();
            try
            {
                if (inDto == null || string.IsNullOrEmpty(inDto.Name))
                {
                    outDto.ErrorMsg = "参数错误";
                    return outDto;
                }

                User user = GetUser(inDto.Id, LoadType.DataBaseDirect);
                if (user == null)
                {
                    outDto.ErrorMsg = "用户不存在";
                    return outDto;
                }

                user.Name = inDto.Name;
                user.Gender = inDto.Gender;
                user.Mobile = inDto.Mobile;
                user.AvailableBalance = inDto.AvailableBalance;

                string remark = "修改用户";
                SystemLog systemLog = LogService.CreateSystemLog(SystemLogType.AlterUser, typeof(User), user.Id, remark);

                PersisterService.SaveObject(new SRO[] { user, systemLog });
                outDto.IsSucceed = true;
            }
            catch (Exception ex)
            {
                outDto.ErrorMsg = ex.Message;
                LogProxy.Error(ex, false);
            }

            return outDto;
        }

        public static DeleteUserOutDTO DeleteUser(DeleteUserInDTO inDto)
        {
            DeleteUserOutDTO outDto = new DeleteUserOutDTO();
            try
            {
                if (inDto == null || string.IsNullOrEmpty(inDto.Id))
                {
                    outDto.ErrorMsg = "参数错误";
                    return outDto;
                }

                User user = UserService.GetUser(inDto.Id, LoadType.DataBaseDirect);
                if (user == null)
                {
                    outDto.ErrorMsg = "用户不存在";
                    return outDto;
                }

                user.IsDeleted = true;

                string remark = "删除用户";
                SystemLog systemLog = LogService.CreateSystemLog(SystemLogType.DeleteUser, typeof(User), user.Id, remark);

                PersisterService.SaveObject(new SRO[] { user, systemLog });
                outDto.IsSucceed = true;
            }
            catch (Exception ex)
            {
                outDto.ErrorMsg = ex.Message;
                LogProxy.Error(ex, false);
            }

            return outDto;
        }

        public static GetUserOutDTO GetUser(GetUserInDTO inDto)
        {
            GetUserOutDTO outDto = new GetUserOutDTO();
            try
            {
                if (inDto == null || string.IsNullOrEmpty(inDto.Id))
                {
                    outDto.ErrorMsg = "参数错误";
                    return outDto;
                }

                User user = GetUser(inDto.Id, LoadType.CacheFirst);

                if (user == null)
                {
                    outDto.ErrorMsg = "用户不存在";
                    return outDto;
                }

                outDto.IsSucceed = true;

                outDto.Id = user.Id;
                outDto.Name = user.Name;
                outDto.Gender = user.Gender;
                outDto.Mobile = user.Mobile;
                outDto.AvailableBalance = user.AvailableBalance;
                outDto.CreatedDate = user.CreatedDate;
                outDto.LastAlterDate = user.LastAlterDate;
            }
            catch (Exception ex)
            {
                outDto.ErrorMsg = ex.Message;
                LogProxy.Error(ex, false);
            }

            return outDto;
        }

        internal static User GetUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            User obj = PersisterService.LoadObject<User>(id);

            return obj;
        }
        internal static User GetUser(string id, LoadType loadType)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            User obj = PersisterService.LoadObject<User>(id, loadType);

            return obj;
        }
        internal static User GetUserFromSub(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            var tableName = "UserById" + id.Substring(id.Length - 1);
            User obj = PersisterService.LoadObject<User>("UserSub",
                tableName, id, LoadType.CacheFirst, true);

            return obj;
        }
        internal static User[] GetUserByHour(int hour)
        {
            var tableName = "UserByHour" + hour.ToString();
            User[] objs = PersisterService.LoadObjects<User>("UserSub",
                tableName, 0, null, null, false, LoadType.CacheFirst);

            return objs;
        }
    }
}
