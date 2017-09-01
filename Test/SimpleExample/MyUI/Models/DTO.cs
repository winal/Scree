using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Scree.Attributes;

namespace MyUI.Models
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

    #region News
    public class AddNewsInDTO : InDTO
    {
        public string Title { get; set; }
        public string Context { get; set; }
        public string Author { get; set; }
        public NewsType Type { get; set; }
    }
    public class AddNewsOutDTO : OutDTO
    {
        public string Id { get; set; }
    }

    public class AlterNewsInDTO : InDTO
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Context { get; set; }
        public string Author { get; set; }
        public NewsType Type { get; set; }
    }
    public class AlterNewsOutDTO : OutDTO
    {

    }

    public class DeleteNewsInDTO : InDTO
    {
        public string Id { get; set; }
    }
    public class DeleteNewsOutDTO : OutDTO
    {
    }

    public class ReadingNewsInDTO : InDTO
    {
        public string Id { get; set; }
    }
    public class ReadingNewsOutDTO : OutDTO
    {
        public int ReadingQuantity { get; set; }
    }

    public class GetNewsInDTO : InDTO
    {
        public string Id { get; set; }
    }
    public class GetNewsOutDTO : OutDTO
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Context { get; set; }
        public string Author { get; set; }
        public NewsType Type { get; set; }
        public int ReadingQuantity { get; set; }
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