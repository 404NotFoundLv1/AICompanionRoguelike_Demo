# 任务清单

本文件只列任务，不包含代码实现。每次只选择一个任务交给 Codex，并在 Unity 编译和手动测试通过后再进入下一个任务。

## 准备阶段

- [ ] 创建 `Assets/_Game` 目录结构。
- [ ] 创建 `Assets/_Game/Scripts` 下的模块目录。
- [ ] 创建基础测试场景。
- [ ] 确认项目使用 Universal 2D。
- [ ] 确认 Git 工作流可用。

## v0.1 玩家横版动作原型

- [ ] 新增 `PlayerInputReader`。
- [ ] 新增 `PlayerMovement2D`。
- [ ] 支持左右移动。
- [ ] 支持跳跃。
- [ ] 支持冲刺。
- [ ] 暴露移动、跳跃、冲刺参数到 Inspector。
- [ ] 提供 `IsDashing` 状态。
- [ ] 提供短暂无敌 `IsInvincible`。
- [ ] 搭建 Player 测试对象。
- [ ] 手动测试移动、跳跃、冲刺。

## v0.2 基础战斗与血量系统

- [ ] 新增 `DamageSourceType`。
- [ ] 新增 `DamageInfo`。
- [ ] 新增 `HealthComponent`。
- [ ] 支持最大血量和当前血量。
- [ ] 支持受伤。
- [ ] 支持治疗。
- [ ] 支持死亡事件。
- [ ] 新增 `PlayerCombat2D`。
- [ ] 使用 OverlapBox 或 OverlapCircle 做近战检测。
- [ ] 搭建 DummyEnemy 测试对象。
- [ ] 手动测试玩家攻击敌人。

## v0.2 敌人基础 AI

- [ ] 新增 `EnemyController2D`。
- [ ] 新增 `EnemyAttack2D`。
- [ ] 支持 Idle 状态。
- [ ] 支持 Chase 状态。
- [ ] 支持 Attack 状态。
- [ ] 支持 Dead 状态。
- [ ] 敌人检测玩家。
- [ ] 敌人靠近玩家。
- [ ] 敌人攻击玩家。
- [ ] 敌人死亡后广播事件。
- [ ] 搭建基础 Enemy Prefab。
- [ ] 手动测试敌人追击、攻击、死亡。

## v0.3 AI 队友基础跟随

- [ ] 新增 `CompanionMovement`。
- [ ] 队友保持在玩家身后上方。
- [ ] 队友距离过远时平滑追赶。
- [ ] 队友超过 teleportDistance 时瞬移到玩家附近。
- [ ] 暴露跟随参数到 Inspector。
- [ ] 搭建 Companion Prefab。
- [ ] 手动测试队友跟随。

## v0.3 AI 队友支援攻击

- [ ] 新增 `CompanionSensor`。
- [ ] 新增 `CompanionCombat`。
- [ ] 检测附近敌人。
- [ ] 选择最近敌人。
- [ ] 队友按冷却攻击。
- [ ] Companion 伤害使用 `DamageInfo`。
- [ ] Companion 伤害来源必须是 `Companion`。
- [ ] 手动测试队友攻击敌人。

## v0.4 AI 不能击杀敌人的保护阈值

- [ ] 新增或修改敌人血量限制逻辑。
- [ ] Companion 来源伤害最多将普通敌人打到 20% 血量。
- [ ] 敌人低于或等于阈值时 Companion 伤害无效。
- [ ] 玩家来源伤害不受限制。
- [ ] 第一次达到阈值时广播补刀请求。
- [ ] 使用 Debug 输出补刀请求。
- [ ] 手动测试 AI 不能击杀敌人。
- [ ] 手动测试玩家可以击杀敌人。

## v0.5 QTE 连携系统

- [ ] 新增 `QTEManager`。
- [ ] 新增 `CompanionQTERequester`。
- [ ] 定义 QTE 结果类型。
- [ ] 支持 QTE 持续时间。
- [ ] 支持正确输入 Success。
- [ ] 支持错误输入 WrongInput。
- [ ] 支持超时 Ignored。
- [ ] 广播三种结果事件。
- [ ] 使用 Debug 输出 QTE 结果。
- [ ] 手动测试三种 QTE 结果。

## v0.6 信赖值、好感度和记忆系统

- [ ] 新增 `CompanionRelationship`。
- [ ] 维护 Trust。
- [ ] 维护 Affection。
- [ ] 限制数值范围为 0 到 100。
- [ ] 支持隐藏评价标签。
- [ ] QTE 成功增加 Trust 和 Affection。
- [ ] QTE 按错降低 Trust 和 Affection。
- [ ] QTE 忽略降低 Affection。
- [ ] 广播关系变化事件。
- [ ] 手动测试关系变化。

## v0.7 基础房间闯关流程

- [ ] 新增 `RunManager`。
- [ ] 新增 `RoomManager`。
- [ ] 定义房间类型。
- [ ] 支持 BattleRoom。
- [ ] 支持 SafeRoom。
- [ ] 支持 ShopRoom。
- [ ] 支持 EliteRoom。
- [ ] BattleRoom 生成若干敌人。
- [ ] 敌人全部死亡后触发 RoomCleared。
- [ ] RoomCleared 后显示或模拟下一房间选择。
- [ ] 手动测试连续房间流程。

## v0.8 濒死保护与分歧事件房

- [ ] 新增 `BondRescueSystem`。
- [ ] 玩家受到致命伤害时触发死亡前检查。
- [ ] Trust 达标时拦截死亡。
- [ ] 每局只触发一次救援。
- [ ] 救援后玩家血量设为 1。
- [ ] 救援后进入 BranchEventRoom。
- [ ] 临时显示挽救选项。
- [ ] 临时显示直接离开选项。
- [ ] 临时显示直面挑战选项。
- [ ] 手动测试濒死救援。

## v0.9 闯关途中动态交流

- [ ] 新增 `DialogueTriggerManager`。
- [ ] 新增 `LocalDialogueTemplate`。
- [ ] 监听房间清怪事件。
- [ ] 监听玩家连续受击事件。
- [ ] 监听玩家连招失败事件。
- [ ] 监听 QTE 结果事件。
- [ ] 战斗中显示短句。
- [ ] 清怪后显示三个玩家回复选项。
- [ ] 玩家回复影响 Trust。
- [ ] 玩家回复影响 Affection。
- [ ] 玩家回复影响隐藏评价。
- [ ] 增加对话冷却。
- [ ] 手动测试本地模板对话。

## v1.0 家园结算与基础技能树

- [ ] 新增家园结算流程。
- [ ] 展示本局关键事件摘要。
- [ ] 展示 Trust 和 Affection 变化。
- [ ] 展示 AI 队友本局评价。
- [ ] 新增基础技能树数据。
- [ ] 新增基础技能树界面。
- [ ] 支持选择或解锁基础成长。
- [ ] 手动测试局外结算。

## v1.1 LLM 对话接入

- [ ] 设计 LLM 请求上下文结构。
- [ ] 设计 LLM 返回结构。
- [ ] 新增 LLM 适配层。
- [ ] 将本地事件转换为 LLM 上下文。
- [ ] 校验 LLM 返回内容。
- [ ] LLM 失败时回退本地模板。
- [ ] 替换部分本地对话模板。
- [ ] 生成记忆摘要。
- [ ] 手动测试联网失败回退。
- [ ] 手动测试 LLM 对话效果。
