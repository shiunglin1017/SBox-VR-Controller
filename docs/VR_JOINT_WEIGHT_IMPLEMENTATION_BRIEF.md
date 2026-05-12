# VR 抓取系統 Joint/Weight 一頁版

## 這份在講什麼
目前專案如何用 Joint + Weight 做出「抓得住、跟得上、又有重量感」的 VR 手感。

## 核心結論（先看這裡）
- `ItemJoint` 負責「物品怎麼跟手走」。
- `_handAnchorJoint`（Physical Hand）負責「手會不會被重物拖」。
- `WeightProfile` 同時影響物理跟隨、IK 追隨、丟擲速度。
- 所以重量不是只改一個數值，而是一次影響整體手感。

## 實作拆解

### 1) 手 ↔ 物品：`ItemJoint`
- 抓取時建立 `PhysicsJoint.CreateFixed(p1, p2)`。
- 透過 `SpringLinear/SpringAngular` 保留彈性，不是完全硬鎖。
- 錨點可接在：
  - `JointPoint`（一般模式）
  - `Body`（Physical Hand 模式）

### 2) 手 ↔ 控制器：`_handAnchorJoint`
- 只在 `UsePhysicalHand = true` 啟用。
- 依 Light/Medium/Heavy 調整 frequency/damping。
- 重物時手更容易出現「被拖慢」的真實感。

### 3) 重量怎麼來
- 先看 tag：`vr_weight_light/medium/heavy`。
- 沒 tag 再看 `Rigidbody.Mass`。
- 得到 `GrabWeightProfile`（位置追隨、旋轉追隨、丟擲 clamp 等參數）。

### 4) 重量影響四個地方
1. `ItemJoint` spring 強度（跟手快慢）
2. hand anchor 剛性（手被拖感）
3. IK stabilizer（轉向與追隨穩定度）
4. 丟擲 clamp（出手速度上限）

## 執行流程（3 步）
1. **抓取**：算 profile → 建 `ItemJoint` → 套 spring。  
2. **持有中**：每幀更新 profile/joint/IK（含雙手修正）。  
3. **放手**：套用 throw estimate + clamp → 清 joint → 手回 Light profile。  

## 為什麼這樣有彈性
- 互動規則、姿勢、物理分層清楚。
- 新增物品時多半只調 profile/provider，不必重寫抓取邏輯。
- 可同時支援輕快道具與重型武器，不用分裂兩套系統。

## 參考程式
- `Code/Player/VrhandInteraction.cs`
- `Code/Player/Services/MassBasedWeightProfileProvider.cs`
- `Code/VRLogic/GrabWeightProfile.cs`
- `Code/Player/Services/RotationLimitedHandPoseStabilizer.cs`
- `Code/Player/Services/PeakThrowVelocityEstimator.cs`
