# README

## 命令行参数

`--config_path`

* 配置文件路径，默认为"config.json"

* 示例

  ```shell
  ./elevation-generator.exe --config_path config.json
  ```

## 配置文件

使用json格式来组织配置信息，由以下几个部分构成

其中具有默认值的字段可以``选择设置为null``或``不配置该字段``来启用默认值

配置文件样例见[附录](#配置文件样例)

* GlobalConfig：全局配置

  | 字段       | 类型                                                         | 含义                          | 默认值   |
  | ---------- | ------------------------------------------------------------ | ----------------------------- | -------- |
  | eye_dir    | json对象<br />{"x":number, <br />"y":number,<br /> "z":number} | 视线方向                      | 无默认值 |
  | up         | json对象<br />{"x":number, <br />"y":number,<br /> "z":number} | 视线上方向                    | 无默认值 |
  | scale_size | number                                                       | 使用clipper进行裁剪时的分辨率 | 4096     |

* ObjConfig：obj文件设置

  | 字段          | 类型   | 含义                                                         | 默认值   |
  | ------------- | ------ | ------------------------------------------------------------ | -------- |
  | path          | string | 模型文件的路径，需保证后缀名为ifc或get<br/>后缀名为ifc代表ifc模型文件<br/>后缀名为get代表由项目生成的用于加速读取的二进制文件 | 无默认值 |
  | current_floor | string | 需要输出的楼层名字                                           | 无默认值 |
  | high_floor    | string | 需要输出的楼层上一层的楼层名字                               | 无默认值 |

* BoxConfig：裁剪包围盒设置

  | 字段  | 类型   | 含义                                          | 默认值                    |
  | ----- | ------ | --------------------------------------------- | ------------------------- |
  | x_min | number | 包围盒x的最小值                               | obj文件中顶点的x最小值-10 |
  | x_max | number | 包围盒x的最大值                               | obj文件中顶点的x最大值+10 |
  | y_min | number | 包围盒y的最小值                               | obj文件中顶点的y最小值-10 |
  | y_max | number | 包围盒y的最大值                               | obj文件中顶点的y最大值+10 |
  | z_min | number | 包围盒z的最小值                               | obj文件中顶点的z最小值-10 |
  | z_max | number | 包围盒z的最大值                               | obj文件中顶点的z最大值+10 |
  | angle | number | （deprecated）包围盒饶y轴旋转的角度（角度制） | 0                         |

* GlConfig：使用OpenGL进行完全遮挡裁剪的配置

  | 字段    | 类型   | 含义                 | 默认值 |
  | ------- | ------ | -------------------- | ------ |
  | gl_size | number | OpenGL渲染时的分辨率 | 4096   |

* ClipConfig：使用clipper进行部分遮挡面裁剪的配置，目前无可配置项

* MergeConfig：使用clipper进行三角面片融合的配置

  | 字段              | 类型   | 含义                                                         | 默认值    |
  | ----------------- | ------ | ------------------------------------------------------------ | --------- |
  | apporx_merge_mode | bool   | （deprecated）启用近似融合模式<br />融合时，对于坐标相近的顶点认为是同一顶点 | false     |
  | merge_mode        | string | （deprecated）三角面片属于同一构件的判定条件设置<br />可选项：“default", "group", "id"<br />”default"、"group"：该条件下obj文件中属于同一object的三角面片为同一构件<br />“id"：使用三角面片id来区分，即该配置下每个三角面片即为一个构件 | "default" |

* SvgConfig：最终导出的svg格式文件的相关配置

  | 字段       | 类型   | 含义                                                         | 默认值  |
  | ---------- | ------ | ------------------------------------------------------------ | ------- |
  | save_path  | string | 导出文件保存的位置                                           | ”1.svg" |
  | image_size | number | svg文件分辨率<br />（仅改变打开svg文件时的默认分辨率，<br />使用软件缩放时仍具有矢量图特性）<br />值为null时保证坐标比例与输入一致且颠倒y轴坐标使得适配dwg转换工具 | null    |

* DebugConfig：与测试相关的配置

  | 字段       | 类型 | 含义                             | 默认值 |
  | ---------- | ---- | -------------------------------- | ------ |
  | print_time | bool | 是否打印运行时间（输出至log.txt) | false  |

## 附录

### 配置文件样例

```json
{
    "ObjConfig":{
        "path":"a.ifc",
        "current_floor":"Floor_2",
        "high_floor":"Floor_3"
    },
    "BoxConfig": {
        "x_min": null,
        "x_max": null,
        "y_min": null,
        "y_max": null,
        "z_min": null,
        "z_max": null,
        "angle": null
    },
    "GlConfig":{
        "gl_size":16384
    },
    "ClipConfig":{
        
    },
    "MergeConfig":{
        "apporx_merge_mode":false,
        "merge_mode":null
    },
    "SvgConfig":{
        "image_size":4096,
        "save_path":"./svg/a.svg"
    },
    "GlobalConfig": {
        "eye_dir": {
          "x": 0,
          "y": 0,
          "z": -1
        },
        "up": {
          "x": 0,
          "y": 1,
          "z": 0
        },
        "scale_size": 34359738368
    },
    "DebugConfig":{
        "print_time": true
    }
}
```

