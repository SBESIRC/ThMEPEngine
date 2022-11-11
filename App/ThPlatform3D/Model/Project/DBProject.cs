using SqlSugar;
using System;
using System.Collections.Generic;

namespace ThPlatform3D.Model.Project
{
    [SugarTable("AI_project")]
    public class DBProject
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 项目编号
        /// </summary>
        public string PrjNo { get; set; }
        /// <summary>
        /// 项目名称
        /// </summary>
        public string PrjName { get; set; }
        public string DesignTypeName { get; set; }
        /// <summary>
        /// 专业名称（暂时没有用到）
        /// </summary>
        public string MajorName { get; set; }
        /// <summary>
        /// 项目创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 创建人Id(协同用户Id)
        /// </summary>
        public string ExecutorId { get; set; }
        /// <summary>
        /// 创建人名称
        /// </summary>
        public string ExecutorName { get; set; }
        /// <summary>
        /// 项目子项信息
        /// </summary>
        [SugarColumn(IsIgnore =true)]
        public List<DBSubProject> SubProjects { get; set; }
    }
}
