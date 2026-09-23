# SOLVE — 工艺参数反解

> 给定输出目标反推可调工艺参数（工艺调机场景："要把不良率压到 4.0，模具温度该设多少？"）。支持多目标、多可调参数、边界约束、可达性判定与确定性复现。
>
> **列角色由表头前缀自动识别**（不区分大小写；未识别列如批次号/日期自动忽略）：
> `Incoming*`/`来料*` = 已知条件（请求行必填）；`Variable*`/`可调*`/`变量*` = 待求解参数（请求行留空）；`Fixed*`/`固定*` = 固定参数（留空取历史中位数）；`Output*`/`输出*` = 输出（请求行填目标值）；`SharedOutput*`/`共享输出*` = 同组共享一条速率的输出（见 [rate 速率模型](#solve-rate)）。
>
> **函数索引**：[INVERSE](#solve-inverse) · [PREDICT](#solve-predict) · [QUALITY](#solve-quality) · [EQUATION](#solve-equation) · [rate/rate_poly 速率模型](#solve-rate)

### 示例数据集（可直接粘贴到 A1:C8）

|   | A (IncomingA) | B (VariableU1) | C (OutputY1) |
|---|---|---|---|
| **1** | IncomingA | VariableU1 | OutputY1 |
| **2** | 1 | 5 | 10 |
| **3** | 2 | 8 | 15 |
| **4** | 3 | 11 | 20 |
| **5** | 4 | 4 | 10 |
| **6** | 5 | 7 | 15 |
| **7** | 6 | 10 | 20 |
| **8** | 10 | *（留空）* | 13 *（目标）* |

第 2–7 行是历史行（满足 `OutputY1 = 2 + 0.5·IncomingA + 1.5·VariableU1`）；
第 8 行是请求行：`IncomingA` 已知为 10，`VariableU1` 留空表示待求解，`OutputY1=13` 是目标值。

---

<a id="solve-inverse"></a>

### SOLVE.INVERSE — 工艺参数反解

**语法**：`=SOLVE.INVERSE(data, [request], [bounds], [model], [seed], [max_starts])`

**示例**：
```
=SOLVE.INVERSE(A1:C8)
```
返回（Excel 365 自动溢出；旧版 Excel 先选好足够大的区域再按 Ctrl+Shift+Enter）：

| 请求行 | VariableU1 | OutputY1预测 | 最大偏差σ | 状态 |
|--------|-----------|--------------|-----------|------|
| 数据第8行 | 4.000 | 13.000 | 0.00 | 可达 |

**结果解读**：
- `VariableU1 = 4` 时预测输出恰为目标 13（手算：`u=(13−2−0.5×10)/1.5=4`）。
- `最大偏差σ = max|预测−目标| / 历史输出标准差`：≈0 = 精确命中；明显大于 0.1 时先用 `SOLVE.QUALITY` 复核模型可信度。
- `状态`：`可达` = 目标落在参数边界内的可达区间；`不可达` = 取到边界也无法达到，推荐值为边界上"最接近"的解。
- **请求表分开写**：`=SOLVE.INVERSE(A1:C7, F1:H2)`（`request` 提供后 `data` 视为纯历史，请求表需同表头）。
- **自定义边界**：`=SOLVE.INVERSE(A1:C8,,{"VariableU1",4,4.5})` 将 `VariableU1` 限制在 [4, 4.5]；两列形式 `{4,4.5}` 按变量列顺序。
- 请求行可调列**填数字 = 该值作为寻优初值**（不会报错，也不会被静默丢弃）。
- 固定参数留空 → 历史中位数；来料留空 → `#VALUE!`（来料是已知条件，必须提供）。

---

<a id="solve-predict"></a>

### SOLVE.PREDICT — 正向预测

**语法**：`=SOLVE.PREDICT(data, values, [model])`

**示例**（试算"来料 10、可调 4"）：
```
=SOLVE.PREDICT(A1:C8, {10,4})
```
返回 `13`（1×1 矩阵）。`values` 按"非输出列"顺序给出（来料 + 可调 + 固定）。

---

<a id="solve-quality"></a>

### SOLVE.QUALITY — 模型质量

**语法**：`=SOLVE.QUALITY(data, [model], [seed])`

**示例**：
```
=SOLVE.QUALITY(A1:C8)
```
返回：

| 输出 | 候选 | CV方案 | CV_R2 | CV_MAE | 选用 |
|------|------|--------|-------|--------|------|
| OutputY1 | linear | LOO | 1.0000 | 0.0000 | 是 |
| OutputY1 | poly | 跳过 | — | — | 否 |
| OutputY1 | rate | 跳过 | — | — | 否 |

**结果解读**：
- `auto`（默认）在 linear / poly / rate 候选中按交叉验证 R² 选优（差值 <1e-9 时优先 linear → poly → rate）；候选结构不可用或样本不足时显示 `跳过`（poly 展开超 100 项、无时间列/配对、或 n<5 的交叉验证下限）。
- `CV方案`：n ≥ 20 用 `5折`，n < 20 用 `LOO`（留一法）。
- R² 越接近 1、MAE 越小，反解结果越可信；R² < 0.8 时建议增加历史数据或显式改用 `model="poly"`。

---

<a id="solve-equation"></a>

### SOLVE.EQUATION — 方程输出

**语法**：`=SOLVE.EQUATION(data, [model])`

**示例**：
```
=SOLVE.EQUATION(A1:C8)
```
返回：

| 输出 | 类型 | 表达式 |
|------|------|--------|
| OutputY1 | 前向方程 | `OutputY1 = 2 + 0.5*IncomingA + 1.5*VariableU1` |
| OutputY1 | 反解公式 | `VariableU1 = (OutputY1 - 2 - 0.5*IncomingA) / 1.5` |

**结果解读**：前向方程可直接手算核对（系数 6 位有效数字）；单变量线性模型额外给出解析反解公式，多变量/高次仅给前向方程。

---

<a id="solve-rate"></a>

### SOLVE 速率模型（rate / rate_poly）— 时间外推

当工艺是「输出 = 来料 − 时间 × 去除速率」的速率过程时，用 `model="rate"`（g 线性）或 `model="rate_poly"`（g 二次，拟合曲率）：

```
OutputZx(t) = IncomingZx − t · g(Bow, 可调, 固定, 其他来料)
```

- **配对**：`OutputZ1` 自动找 `IncomingZ1`（去掉角色前缀后同后缀，`输出收率`↔`来料收率`）；配对缺失时显式速率模型返回 `#VALUE!`。
- **时间列（可多个，ADR-0009）**：去掉角色前缀后名称含 `Time`/`时间` 的列。多输出各用各的时间列时按后缀配对（`FixedTimeZ1` ↔ `OutputZ1`）；只有一个时间列时对所有输出全局生效（旧表用法不变）；多个时间列但某输出对不上 → 显式速率模型 `#VALUE!`。所有时间列都不进入 g；t 必须有限且 >0。
- **时间可调**：列名写成 `VariableTime`（或 `可调时间`）并在请求行留空 → 求解器把时间也当作可调参数（用 bounds 放宽范围，如 `{"VariableTime",60,120}`）；`FixedTime` 则按给定值/历史中位数。
- **共享速率（`SharedOutput*`/`共享输出*`，ADR-0009）**：多个共享输出列共用一条 g（池化拟合），适合"同机理、多响应"的数据；组内成员须全部配对来料/时间，auto 在共享组内比较 `rate` 与 `rate_poly`。
- **额外输出**：速率生效时 `SOLVE.INVERSE` 增加每输出一列 `<输出名>速率`（单位：输出单位/时间单位，同组数值相同）；`SOLVE.EQUATION` 增加 `速率方程` 行（rate_poly 含平方/交互项）。

**示例**（粘贴到 A1:D8；速率律 `r = 0.01 + 0.005·VariableU1`，t ∈ {30, 60}s）：

|   | A (IncomingZ1) | B (VariableU1) | C (FixedTime) | D (OutputZ1) |
|---|---|---|---|---|
| **1** | IncomingZ1 | VariableU1 | FixedTime | OutputZ1 |
| **2** | 10.1 | 2 | 30 | 9.5 |
| **3** | 10.2 | 3 | 60 | 8.7 |
| **4** | 10.3 | 4 | 30 | 9.4 |
| **5** | 10.4 | 5 | 60 | 8.3 |
| **6** | 10.5 | 6 | 30 | 9.3 |
| **7** | 10.6 | 7 | 60 | 7.9 |
| **8** | 10 | *（留空）* | 90 *（外推时间）* | 7.3 *（目标）* |

```
=SOLVE.INVERSE(A1:D8)                 // auto 自动选中 rate（线性无法解释 t 交互）
```

| 请求行 | VariableU1 | OutputZ1预测 | OutputZ1速率 | 最大偏差σ | 状态 |
|--------|-----------|--------------|--------------|-----------|------|
| 数据第8行 | 4.000 | 7.300 | 0.0300 | 0.00 | 可达 |

- 手算核对：`r = 0.01+0.005×4 = 0.03`，`Output(90) = 10 − 90×0.03 = 7.3`。
- 试算其它时间：`=SOLVE.PREDICT(A1:D8, {10,4,120})` → `6.4`。
- 方程：`=SOLVE.EQUATION(A1:D8)` → `OutputZ1(FixedTime) = IncomingZ1 - FixedTime*(0.01 + 0.005*VariableU1)` 与 `OutputZ1速率 = 0.01 + 0.005*VariableU1`。
- 若要让求解器**自己选时间**：把 C 列表头改为 `VariableTime`，请求行 C 留空，`=SOLVE.INVERSE(A1:D8,,{"VariableTime",60,120})`。
- **二次速率**：`=SOLVE.INVERSE(A1:D8,,,"rate_poly")`（g 含平方/交互项，适合速率随参数弯曲的数据；样本需 ≥ 展开项数+1）。
- **多时间列**：为不同输出各配一个时间列（如 `FixedTimeZ1`、`FixedTimeZ2`），求解器按后缀自动配对、各自外推。
- **共享速率**：把输出列命名为 `SharedOutputZ1`、`SharedOutputZ2`（各配 `IncomingZ1`/`IncomingZ2`），两列共享一条 g（适合多种响应同一去除机理），`SOLVE.INVERSE` 的速率列给出相同速率值。

---

<a id="solve-notes"></a>

### SOLVE 使用注意

- **模型范围**：`linear` / `poly`（二次含两两交互）/ `rate`（线性速率 + 时间外推）/ `rate_poly`（二次速率 + 时间外推）；强非线性（阶跃/强交互）数据请先看 `SOLVE.QUALITY`。
- **速率分组**：`SharedOutput*` 列必须全部配对来料/时间；共享组仅比较 `rate`/`rate_poly`（池化 CV）。若成员机理不同，池化会平均掉差异——分组前先用 `SOLVE.QUALITY` 确认。
- **样本下限**：`auto` 选型与 `SOLVE.QUALITY` 依赖交叉验证，历史行须 ≥5，否则返回 `#VALUE!`；显式 `model="linear"` 仅需 行数 ≥ 展开项数+1（单变量线性最少 2 行），小样本可显式指定模型。
- **auto 候选跳过**：某候选的交叉验证失败（如设计矩阵精确共线/秩亏）时按 `跳过` 处理并选可用候选；全部候选不可用才 `#VALUE!`（推荐消除共线列或显式指定 `model`）。
- **poly 外推**：边界默认历史最小/最大；推荐值触界时需实验确认。
- **确定性**：同 `seed`（默认 42）两次调用结果完全一致；`max_starts` 默认 10（上限 50）。
- **规模上限**（超限返回 `#VALUE!`）：历史 5000 行 / 请求 200 行 / 特征列 50 / 可调参数 20 / 输出 20 / poly 展开 100 项。

---
