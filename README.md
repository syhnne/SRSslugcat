
一些游玩建议：从外层空间出来你只能走地下，因为根源设施有水。此时你有两个选择，一是倒爬地下的裂口（有回响那个地方）进农场阵列再到郊区，二是在地下多绕一段路，然后走排水系统。我改了排水系统的两个房间，让他们连起来，这样排水系统理论上是可以通过的（但靠近郊区的业力门那里有一个特别难过的地方，那个我懒得改了，建议挨饿（）

制作进度过快的话，我会考虑把四个mod合并成一个，一起发布（这样gravityController不用复制四遍了）那个时候我可以顺便重构一下fp那边的代码，解决一些本来用roomsettings就能解决的问题（比如演算室当避难所，和监视者投屏）


不完整的特性列表，包括做完的和正在做的（真没活了啊啊啊啊啊啊啊啊）：
- 可以像矛大师一样拔矛扎东西吃
- 拔出的矛有两种模式，如果玩家不是饥饿状态，那么拔出的矛可以秒杀大部分被击中的生物（打到蜥蜴头部之类的地方不算）你可以通过观察矛的颜色来判断这个功能是否在生效
- 可以吃东西，但食性和白猫相同，且只给四分之一饱食度（但蝠蝇和小蜈蚣之类生物都可以先扎后吃）
- 不能碰水，碰了会当场去世。除非你在挨饿。
- 下悬架的尽头本来是断桥，但那里会有一些新地图，一直连接到moon的结构顶端（原计划是12个房间，但好像变得越来越多了，目前是7/14）
- fp的演算室里有一个氧气面罩（他现在是一个白色的方块），拿着那个东西可以让你不受到水的伤害，并且大幅提高肺活量。请注意在水里往下游的时候千万不要按拾取键。
- 至于他能不能直接说话……我得再斟酌一下，打开游戏之后才意识到猫猫会说话这个事情真的很怪
- 做路上的广播。。一想到要写东西就很难不流汗
- 等有空了再考虑一下tailSpeckles那个东西要怎么修吧，，它肯定会很麻烦，因为还涉及到矛的颜色问题

已知问题：
- （已修复）tailspecks的贴图不能正常显示。泪目了，原来无论tailspecks是什么颜色，拔出来的矛都是白色的。还得改啊。





啊哈哈哈，只要我想个办法多整点珍珠对话文本来，玩家就不得不在矛大师同款单手开荒和饥一顿饱一顿之间做出选择了（发出邪恶的笑声

乐，我花了一晚上研究为什么我的自定义地区地图修改了之后没变化，后来我想起来SBCameraScroll模组是会提前缓存地图的，你不碰他的cache他就不会给你改。怒而禁用之。

懂了，C:\Program Files (x86)\Steam\steamapps\workshop\content\312520\2928752589\world 删掉sl文件夹让它重新加载

但是exclusive部分还是不好使，现在导致的结果是，只要玩家启用这个mod，他们就能进我的自定义区域，但我想要的效果是只有srs能进。我本来照着msc的一个文档的格式抄，但那段无论如何都不生效，喵的这txt文档里还不能输出日志导致我根本不知道是哪个环节出了问题啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊好崩溃

破案了，那个东西是要看worldstate的，我得在json里把worldstate写上才能生效

再讲个笑话，我做了三个图之后他们卡了俩小时bug，最后我查出原因，是我地图名字和游戏内房间重名了。。

下辈子不做地图了。。。