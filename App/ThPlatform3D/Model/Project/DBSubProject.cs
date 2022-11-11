using SqlSugar;

namespace ThPlatform3D.Model.Project
{
    [SugarTable("AI_prjrole")]
    public class DBSubProject
    {
        /// <summary>
        /// 项目Id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 子项Id
        /// </summary>
        public string SubentryId { get; set; }
        /// <summary>
        /// 子项名称
        /// </summary>
        public string SubEntryName { get; set; }
    }
}
