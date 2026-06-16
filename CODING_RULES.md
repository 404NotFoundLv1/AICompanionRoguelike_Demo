# 编码规则

## 基本规则

- 使用 Unity 6、C#、MonoBehaviour 和组件化设计。
- 所有游戏脚本放在 `Assets/_Game/Scripts` 的对应模块目录中。
- 不把脚本散落在 `Assets` 根目录。
- 不创建和当前任务无关的目录。
- 每次只实现一个可独立测试的小功能。
- 不改动和当前任务无关的文件。
- 不在功能没跑通时继续开发下一个功能。

## 模块规则

- 不写巨大 `GameManager`。
- 不把输入、移动、战斗、AI、UI、房间流程写进同一个 MonoBehaviour。
- 系统之间优先通过事件、委托或小接口通信。
- UI 只展示状态和转发用户输入，不承载核心玩法判定。
- Combat 模块必须保持确定性，不能依赖 LLM。
- Dialogue 和 LLM 不能直接控制底层战斗。

## 命名建议

- 玩家移动：`PlayerMovement2D`。
- 玩家输入：`PlayerInputReader`。
- 玩家战斗：`PlayerCombat2D`。
- 血量组件：`HealthComponent`。
- 伤害数据：`DamageInfo`。
- 敌人控制：`EnemyController2D`。
- 敌人攻击：`EnemyAttack2D`。
- 队友移动：`CompanionMovement`。
- 队友感知：`CompanionSensor`。
- 队友战斗：`CompanionCombat`。
- QTE 管理：`QTEManager`。
- 关系系统：`CompanionRelationship`。
- 房间管理：`RoomManager`。
- 闯关管理：`RunManager`。

## 配置规则

- 数值优先使用 `[SerializeField]` 暴露到 Inspector。
- 多个对象共享的配置使用 ScriptableObject。
- 不把调参常量散落在多个脚本里。
- 不在代码中硬编码场景对象名称作为核心逻辑依赖。

## 事件规则

- 血量变化、死亡、房间清理、QTE 结果、关系变化等都应广播事件。
- 事件参数应包含足够上下文，例如伤害来源、目标对象、结果类型。
- 事件订阅必须在合适生命周期中解除，避免对象销毁后仍被调用。
- 不要通过 `FindObjectOfType` 在 Update 中反复查找系统。

## 性能规则

- 不在每帧做大量 `Find`、`GetComponent`、全场景遍历或复杂 LINQ。
- 高频逻辑中缓存组件引用。
- 物理检测应使用明确 LayerMask。
- QTE、房间、对话等低频逻辑通过事件触发。

## AI 和 LLM 规则

- AI 队友可以跟随、感知、攻击、请求 QTE。
- AI 队友不能替玩家击杀普通敌人。
- LLM 不直接控制战斗、伤害、死亡、移动、QTE 判定。
- LLM 输出必须先转换为本地可校验的数据结构。
- 前期所有对话使用本地模板，不接 LLM。

## 测试规则

每个任务完成时至少给出：

- 新增或修改的文件路径。
- Inspector 绑定方式。
- 场景对象或 Prefab 创建方式。
- 手动测试步骤。
- 当前不包含哪些功能。

每个模块完成后必须能在 Unity 中编译通过，并能在一个简单场景里独立验证。

## Git 规则

- 一个小功能一个提交。
- 不把多个版本目标混在一个提交里。
- 提交前确认 Unity 无编译错误。
- 提交信息使用清晰英文，例如 `v0.1 player movement prototype`。
