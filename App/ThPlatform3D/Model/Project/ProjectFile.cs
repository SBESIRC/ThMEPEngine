using SqlSugar;
using System;

namespace ThPlatform3D.Model.Project
{
    [SugarTable("ProjectFile")]
    public class ProjectFile
    {
        /// <summary>
        /// 项目文件ID
        /// </summary>
        public string ProjectFileId { get; set; }
        /// <summary>
        /// 协同项目Id
        /// </summary>
        public string PrjId { get; set; }
        /// <summary>
        /// 协同项目子项Id
        /// </summary>
        public string SubPrjId { get; set; }
        /// <summary>
        /// 项目文件名称（不含后缀名）
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// 来源名称（CAD,SU,IFC）
        /// </summary>
        public string ApplicationName { get; set; }
        /// <summary>
        /// 专业名称
        /// </summary>
        public string MajorName { get; set; }
        /// <summary>
        /// 文件夹结构（相对路径，项目内的子文件夹路径）
        /// </summary>
        public string Folder { get; set; }
        /// <summary>
        /// 占用人Id
        /// </summary>
        public string Occupier { get; set; }
        /// <summary>
        /// 占用人Name
        /// </summary>
        public string OccupierName { get; set; }
        /// <summary>
        /// 创建人Id
        /// </summary>
        public string CreaterId { get; set; }
        /// <summary>
        /// 创建人名称
        /// </summary>
        public string CreaterName { get; set; }
        /// <summary>
        /// 更新人Id
        /// </summary>
        public string UpdatedBy { get; set; } = null;
        /// <summary>
        /// 更新人名称
        /// </summary>
        public string UpdatedUserName { get; set; } = null;
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
        [SugarColumn(IsIgnore = true)]
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 是否已删除（0未删除，1已删除）
        /// 数据删除做的是逻辑删除，不是物理删除
        /// </summary>
        public int IsDel { get; set; }
    }
}
