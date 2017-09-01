using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Scree.Attributes;

namespace MyApp.Models
{

    #region DTO
    [Serializable]
    public class InDTO
    {
    }
    [Serializable]
    public class OutDTO
    {
        public bool IsSucceed { get; set; }
        public string ErrorMsg { get; set; }
    }
    #endregion

    #region User
    public class AddUserInDTO : InDTO
    {
        public string Name { get; set; }
        public Gender Gender { get; set; }
        public string Mobile { get; set; }
        public decimal AvailableBalance { get; set; }
    }
    public class AddUserOutDTO : OutDTO
    {
        public string Id { get; set; }
    }

    public class AlterUserInDTO : InDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Gender Gender { get; set; }
        public string Mobile { get; set; }
        public decimal AvailableBalance { get; set; }
    }
    public class AlterUserOutDTO : OutDTO
    {

    }

    public class DeleteUserInDTO : InDTO
    {
        public string Id { get; set; }
    }
    public class DeleteUserOutDTO : OutDTO
    {
    }

    public class GetUserInDTO : InDTO
    {
        public string Id { get; set; }
    }
    public class GetUserOutDTO : OutDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Gender Gender { get; set; }
        public string Mobile { get; set; }
        public decimal AvailableBalance { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastAlterDate { get; set; }
    }
    #endregion

    #region Query
    public class QueryInDTO : InDTO
    {
        public int PageSize { get; set; }
        public int PageIndex { get; set; }
        public string Condition { get; set; }
    }
    public class QueryOutDTO : OutDTO
    {
        public int PageCount { get; set; }
        public int RecordCount { get; set; }
        public string Body { get; set; }
    }
    #endregion
}