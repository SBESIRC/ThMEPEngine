using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{
    class Tools
    {
        //public static T Clone<T>(T RealObject)
        //{
        //    using (Stream objectStream = new MemoryStream())
        //    {
        //        IFormatter formatter = new BinaryFormatter();
        //        formatter.Serialize(objectStream, RealObject);
        //        objectStream.Seek(0, SeekOrigin.Begin);
        //        return (T)formatter.Deserialize(objectStream);
        //    }
        //}

        //public static T DeepCopy<T>(T obj)
        //{
        //    object retval;
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        XmlSerializer xml = new XmlSerializer(typeof(T));
        //        xml.Serialize(ms, obj);
        //        ms.Seek(0, SeekOrigin.Begin);
        //        retval = xml.Deserialize(ms);
        //        ms.Close();
        //    }
        //    return (T)retval;
        //}


        ////https://www.quarkbook.com/?p=1210
        ////深拷贝性能检测
        ////UserInfo newInfo = TransExp<UserInfo, UserInfo>.Trans(info)
        //public static class TransExp<TIn, TOut>
        //{
        //    private static readonly Func<TIn, TOut> cache = GetFunc();
        //    private static Func<TIn, TOut> GetFunc()
        //    {
        //        ParameterExpression parameterExpression = Expression.Parameter(typeof(TIn), "p");
        //        List<MemberBinding> memberBindingList = new List<MemberBinding>();

        //        foreach (var item in typeof(TOut).GetProperties())
        //        {
        //            if (!item.CanWrite) continue;
        //            MemberExpression property = Expression.Property(parameterExpression, typeof(TIn).GetProperty(item.Name));
        //            MemberBinding memberBinding = Expression.Bind(item, property);
        //            memberBindingList.Add(memberBinding);
        //        }

        //        MemberInitExpression memberInitExpression = Expression.MemberInit(Expression.New(typeof(TOut)), memberBindingList.ToArray());
        //        Expression<Func<TIn, TOut>> lambda = Expression.Lambda<Func<TIn, TOut>>(memberInitExpression, new ParameterExpression[] { parameterExpression });

        //        return lambda.Compile();
        //    }

        //    public static TOut Trans(TIn tIn)
        //    {
        //        return cache(tIn);
        //    }
        //}

        ////UserInfo newInfo = mapper.Map<UserInfo>(info);
        ////using AutoMapper;

        ////浅拷贝
        ////public Person ShallowCopy()
        ////{
        ////    return (Person)this.MemberwiseClone();
        ////}

    }
}
