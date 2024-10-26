# NareisLib[1.4/1.5]
# !!!由于1.5版本更新后渲染机制大幅度更改，目前的Wiki版本仅适用于1.4版本，1.5版本更完善的Wiki正在编写中。届时也会补充1.4版本Wiki中的不足部分!!!




# !以下部分内容仅适用于1.4版本!
## 这是一个基于原版和外星人模组HumanAlienRace（以下简称HAR）的自定义多层级渲染模组

### 1.针对原版的Def定义其图像
例如：  
* 对于一个`defName`为`Test_Hair`的`HairDef`；  
* 我们在另一个xml文件中定义一个`MultiTexDef`，`defName`随意，设置其`originalDef`属性为`Test_Hair`;  
* 这表示此`MultiTexDef`是针对这个名为`Test_Hair`的`HairDef`创建的，现在它们被关联在了一起；  
* 我们可以在`MultiTexDef`里设置许多图层，当游戏渲染我们关联的`HairDef`的时候，就会自动检测到我们在`MultiTexDef`里定义的图层并一起渲染。  
    （此为大致举例流程，实际使用请看[wiki操作介绍](https://github.com/Kamijouko/NareisLib/wiki)） 
  
  可以作用于Pawn身上的Def包括：`BodyDef`，`HeadDef`，`HairDef`，`Apparel`(衣服)，`HandTypeDef`。  
### 2.自定义多层图像  
如字面意思，可以在MultiTexDef中定义多个相同或不同图层的图像；  

  #### 但在列举图层之前需要先简单了解一些RimWorld对于Pawn(小人)的渲染机制：  
    1.RimWorld对于Pawn的渲染主要使用来自Unity引擎的DrawMesh函数；  
    2.一个Pawn大致由两个部分组成，即头(Head)和身子(Body)；  
    3.而如同函数字面意思，每个部分对应一种Mesh(网格)，贴图需要绘制在Mesh上，Mesh来自游戏内部的网格池或者即时创建；  
    ps：原版RimWorld无法渲染非常大的贴图即是因为网格的大小限制；  
    4.原版在绘制网格时，每层网格的间距非常小(小数点后两个零到三个零左右)而在间距小于gpu能处理的最高精度时，将由函  
    数的执行顺序决定渲染顺序(即先执行的在底部)；  
    5.Rimworld的的渲染轴相较于传统unity游戏的区别是，它使用y轴作为高度轴（正常情况下为z）,即x和z为图像的宽和高，  
    y确定该图像的显示高度。
  #### 本模组通过对原版的渲染函数进行补充和修改，定义了一些可以指定的图层：  
    图层高度顺序由上往下递增  
    1.BottomOverlay  
    2.BottomHair  
    3.BottomShell  
    4.Body    
    5.Apparel  
    6.Hand    
    7.Head  
    8.FaceMask  
    9.Hair  
    10.FrontShell  
    11.HeadMask  
    12.Hat  
    13.Overlay  
其中的Body、Apparel、Head、FaceMask、Hair均为原版Pawn的身体部件，它们分别代表Pawn的身体、服装/装备/头部装备、头、脸妆/胡须、头发，而其他图层则是围绕这几个部件展开的。它们的默认渲染顺序也同上方展示的一致。  
以上列出的图层并不是指最多只能设置13个层，以上层所起的作用是确定图层的y轴偏移基准。  
通过在xml里指定图像为以上的任意图层，然后修改它的各项属性，由此达到多图层渲染的目的即为本模组的功能。  
### 3.已知问题  
与大部分修改了渲染逻辑的模组冲突；  
已知冲突的有：  
> [Hats Display Selection](https://steamcommunity.com/sharedfiles/filedetails/?id=1542291825)  
> [[CAT]Show Hair With Hats or Hide All Hats](https://steamcommunity.com/sharedfiles/filedetails/?id=2879080074)  
  
针对模组不兼容，目前此框架使游戏内的所有头发强制显示，后续会加入自定义设置。
  
    



  
  
  




