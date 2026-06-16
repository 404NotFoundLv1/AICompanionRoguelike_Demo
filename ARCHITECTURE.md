# 架构说明

## 总体原则

本项目采用模块化、组件化、事件驱动的 Unity 2D 架构。每个系统只负责自己的边界，不通过一个巨大 `GameManager` 串起所有逻辑。

底层玩法必须可预测、可测试、可复现。AI 队友和后续 LLM 只影响高层行为、表达和选择建议，不直接决定每一帧移动、攻击、伤害或死亡。

## 推荐目录

```text
Assets/_Game
├── Scenes
├── Scripts
│   ├── Core
│   ├── Character
│   ├── Combat
│   ├── Enemy
│   ├── Companion
│   ├── QTE
│   ├── Roguelike
│   ├── Dialogue
│   ├── Memory
│   ├── Home
│   ├── UI
│   └── Save
├── Prefabs
├── ScriptableObjects
├── Art
├── Audio
└── Animations
```

所有游戏脚本必须放在 `Assets/_Game/Scripts` 的对应模块目录中。

## 模块边界

### Core

职责：
- 通用枚举、基础事件、运行时状态、服务接口和轻量工具。
- 不包含具体玩法实现。

不得：
- 承担战斗、敌人、AI、房间、UI 等具体系统逻辑。

### Character

职责：
- 玩家输入读取。
- 横版移动、跳跃、冲刺。
- 玩家状态，如是否冲刺、是否短暂无敌。

依赖：
- 可以依赖 `Combat` 的受击或无敌接口。
- 不依赖 `Enemy`、`Companion`、`Dialogue`、`Home`。

### Combat

职责：
- `HealthComponent`、`DamageInfo`、伤害来源、治疗、死亡。
- 攻击检测、命中处理、无敌帧、死亡前检查。

依赖：
- 可以依赖 `Core`。
- 不依赖 UI、Dialogue、LLM。

### Enemy

职责：
- 敌人状态机。
- 追击、攻击、死亡广播。
- 后续扩展普通敌人、精英、Boss。

依赖：
- 依赖 `Combat`。
- 不直接依赖 `Dialogue` 和 `Home`。

### Companion

职责：
- AI 队友跟随。
- 敌人感知。
- 支援攻击。
- 不能击杀敌人的限制。
- QTE 请求的触发条件。

依赖：
- 可以依赖 `Combat`、`QTE`、`Memory` 的公开事件或接口。
- 不直接控制玩家输入和底层战斗判定。

### QTE

职责：
- QTE 请求、倒计时、输入判定。
- 输出 Success、WrongInput、Ignored 等结果事件。

依赖：
- 可以监听 `Companion` 或 `Enemy` 的状态事件。
- 不直接修改复杂关系、UI 和房间流程，结果通过事件交给其他模块处理。

### Memory

职责：
- 信赖值、好感度、隐藏评价标签。
- 监听 QTE、对话选择、濒死救援等结果。
- 广播关系变化事件。

依赖：
- 可以依赖 `Core` 事件。
- 不直接实现 UI、存档和 LLM 请求。

### Roguelike

职责：
- Run 流程。
- 房间类型。
- BattleRoom 清怪检测。
- RoomCleared 和下一房间选择。
- 分歧事件房入口。

依赖：
- 可以监听 `Enemy` 死亡事件和 `Combat` 结果事件。
- 不负责敌人具体 AI。

### Dialogue

职责：
- 本地模板对话。
- 事件触发短句。
- 清怪后的玩家回复选项。
- 后续 LLM 适配层。

依赖：
- 监听 `QTE`、`Roguelike`、`Memory`、`Combat` 的事件。
- 不直接控制战斗判定。

### Home

职责：
- 局外结算。
- 家园成长。
- 基础技能树。
- AI 陪伴长期反馈。

依赖：
- 读取 `Memory`、`Save`、`Roguelike` 的结果数据。

### UI

职责：
- 血条、QTE 提示、对话框、房间选择、关系显示。
- 只展示状态和转发玩家 UI 输入。

依赖：
- 通过事件订阅各系统。
- 不承载核心战斗逻辑。

### Save

职责：
- 存档结构。
- 玩家成长、AI 关系、家园状态、已解锁内容。

依赖：
- 读取各系统提供的数据快照。
- 不直接驱动战斗。

## 依赖方向

```text
Core
→ Character / Combat
→ Enemy
→ Companion
→ QTE
→ Memory
→ Roguelike
→ Dialogue
→ Home / Save
→ LLM Adapter
```

允许通过事件反向通知结果，但不要让高层模块直接调用底层模块内部实现。

## 关键流程

### 战斗伤害流程

```text
攻击组件检测命中
→ 创建 DamageInfo
→ 目标 HealthComponent.TakeDamage
→ 应用无敌、保护阈值、死亡前检查
→ 广播受击/死亡事件
```

### AI 队友支援流程

```text
CompanionSensor 检测敌人
→ CompanionCombat 选择最近目标
→ 造成 Companion 来源伤害
→ 敌人血量最多降到保护阈值
→ 第一次到阈值时请求玩家补刀
```

### QTE 流程

```text
敌人进入可连携状态
→ CompanionQTERequester 发起请求
→ QTEManager 开始倒计时
→ 玩家按键
→ Success / WrongInput / Ignored
→ Memory、UI、Dialogue 分别响应结果事件
```

### 濒死保护流程

```text
玩家受到致命伤害
→ Combat 广播 OnBeforeDeath 检查
→ BondRescueSystem 判断 Trust 和本局次数
→ 允许救援则玩家血量设为 1
→ Roguelike 进入 BranchEventRoom
```

## LLM 边界

LLM 后期只允许输出：
- 对话文本。
- 玩家回复选项。
- AI 队友高层意图。
- 记忆摘要。
- 局外陪伴反馈。

LLM 不允许直接输出：
- 每帧移动。
- 攻击命中。
- 伤害数值最终判定。
- 敌人死亡。
- QTE 成功失败。
- 存档写入。

所有来自 LLM 的内容必须先被本地系统校验，再作为表现层或建议层使用。
