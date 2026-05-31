# Boxer

**상자를 드래그 방향에 맞춰 올바른 컨베이어 라인으로 보내는 분류 액션 게임**입니다.  
플레이어는 화면을 드래그해 방향을 입력하고, 현재 중앙 분류기에 도착한 상자를 해당 모양에 맞는 방향으로 보내야 합니다. 제한 시간 안에 목표 수량을 처리해야 하며, 잘못 분류하면 HP가 감소합니다.

## 게임 개요

| 항목 | 내용 |
| --- | --- |
| 장르 | 드래그 기반 분류 액션 / 캐주얼 퍼즐 |
| 핵심 조작 | 마우스 드래그 |
| 핵심 목표 | 상자에 표시된 방향 정보를 보고 알맞은 방향으로 분류 |
| 주요 판정 | 8방향 드래그 인식, 상자 타입별 정답 방향 비교 |
| 실패 조건 | 오분류로 HP가 0이 되거나 제한 시간 내 목표 수량을 달성하지 못함 |
| 개발 환경 | Unity 6, C# |
| 주요 구현 범위 | 드래그 입력, 제스처 방향 인식, 상자 큐, 컨베이어 라인, 오브젝트 풀링, 점수/HP/타이머 UI, 성공/실패 연출 |

## 핵심 기획

Boxer의 핵심은 **빠르게 들어오는 상자를 보고, 즉시 알맞은 방향으로 드래그해 분류하는 것**입니다.

상자는 중앙 분류기로 이동하고, 플레이어는 상자에 표시된 방향 또는 표식을 보고 드래그 입력을 수행합니다. 드래그는 8방향 중 하나로 인식되며, 각 상자 타입마다 정해진 정답 방향이 있습니다. 정답 방향으로 보내면 처리 수량이 증가하고, 틀린 방향으로 보내면 HP가 감소합니다.

이 구조는 단순한 버튼 입력이 아니라, 플레이어의 손동작을 입력값으로 사용하기 때문에 다음 요소가 중요합니다.

- 드래그 시작점과 끝점의 방향
- 드래그 전체 궤적의 직선성
- 너무 짧은 입력 무시
- 8방향 분류의 명확성
- 빠른 반복 처리 중 오입력 최소화

## 플레이 루프

```text
상자 생성
  ↓
대기 슬롯에 배치
  ↓
중앙 분류기로 이동
  ↓
플레이어 드래그 입력
  ↓
드래그 궤적을 8방향으로 인식
  ↓
상자 타입의 정답 방향과 비교
  ↓
정답이면 처리 수량 증가
  ↓
오답이면 HP 감소
  ↓
다음 상자 진입
```

## 주요 시스템

### 1. 드래그 입력 시스템

플레이어 입력은 `DragInputController`에서 처리합니다.  
마우스 버튼을 누르면 드래그를 시작하고, 누르고 있는 동안 화면 좌표를 계속 수집한 뒤, 버튼을 떼는 순간 전체 드래그 궤적을 분석합니다.

주요 파일:

```text
Assets/Scripts/InGame/DragInputController.cs
```

입력 흐름:

```text
마우스 다운
  ↓
포인터가 UI 위인지 확인
  ↓
드래그 좌표 수집 시작
  ↓
마우스 홀드 중 좌표 누적
  ↓
마우스 업
  ↓
전체 이동 거리 계산
  ↓
최소 거리보다 짧으면 무시
  ↓
제스처 방향 인식
  ↓
현재 상자를 해당 방향으로 라우팅
```

너무 짧은 드래그는 실수 입력으로 보고 무시합니다.  
드래그가 충분히 길면 제스처 인식 결과와 단순 시작점-끝점 벡터 방향을 함께 사용해 최종 방향을 결정합니다.

### 2. 8방향 제스처 인식

Boxer의 방향 입력은 총 8방향으로 분류됩니다.

```text
South
SouthWest
West
NorthWest
North
NorthEast
East
SouthEast
```

주요 파일:

```text
Assets/Scripts/InGame/Direction8.cs
Assets/Scripts/InGame/AdvancedGestureRecognizer.cs
Assets/Scripts/InGame/GestureDirectionRecognizer.cs
```

`AdvancedGestureRecognizer`는 드래그 궤적을 64개 샘플로 재구성하고, 크기 정규화와 원점 이동을 거친 뒤 8방향 템플릿과 비교합니다. 또한 단순 거리 비교만 하지 않고 다음 요소를 함께 점수화합니다.

- 전체 드래그 경로 길이
- 시작점과 끝점 사이의 직선성
- 드래그 각도와 템플릿 각도의 차이
- 정규화된 경로와 템플릿 사이의 평균 거리

이후 `DragInputController`에서 시작점-끝점 벡터 방향과 제스처 알고리즘 결과가 같거나 인접 방향이면 보너스 점수를 더해 최종 방향을 선택합니다.

### 3. 상자 타입과 정답 방향

상자는 여러 타입으로 나뉘며, 각 타입마다 보내야 하는 방향이 정해져 있습니다.

주요 파일:

```text
Assets/Scripts/InGame/BoxPayloadType.cs
Assets/Scripts/InGame/SorterController.cs
```

상자 타입:

```text
Shape1
Shape2
Shape3
Shape4
Shape5
Shape6
Shape7
Bomb
```

정답 방향 매핑:

| 상자 타입 | 정답 방향 |
| --- | --- |
| Shape1 | SouthWest |
| Shape2 | West |
| Shape3 | NorthWest |
| Shape4 | North |
| Shape5 | NorthEast |
| Shape6 | East |
| Shape7 | SouthEast |
| Bomb | South |

`Bomb` 타입은 일반 컨베이어 방향이 아니라 아래 방향으로 보내는 특수 처리 대상입니다.

### 4. 분류기 라우팅 시스템

`SorterController`는 현재 중앙 분류기에 있는 상자를 보관하고, 드래그로 인식된 방향을 받아 해당 컨베이어 라인으로 보냅니다.

주요 파일:

```text
Assets/Scripts/InGame/SorterController.cs
Assets/Scripts/InGame/ConveyorLane.cs
```

라우팅 흐름:

```text
상자가 분류기에 도착
  ↓
SorterController가 currentBox로 보관
  ↓
드래그 입력으로 Direction8 전달
  ↓
상자 타입별 정답 방향과 비교
  ↓
GameController에 정답 여부 전달
  ↓
해당 방향의 ConveyorLane 선택
  ↓
상자 이동 시작
  ↓
분류기 비움
  ↓
다음 상자 진입
```

### 5. 상자 이동과 연출

상자는 `BoxController`가 상태를 가지고 관리합니다.

주요 파일:

```text
Assets/Scripts/InGame/BoxController.cs
```

상자 상태:

| 상태 | 의미 |
| --- | --- |
| InSlot | 대기 슬롯에 있음 |
| MovingToTarget | 슬롯 또는 분류기로 이동 중 |
| AtSorter | 중앙 분류기에 도착 |
| MovingOnLane | 컨베이어 라인을 따라 이동 중 |
| Dropping | 아래 방향으로 떨어지는 중 |

상자는 대기 슬롯에서 중앙 분류기로 이동하고, 분류가 완료되면 선택된 컨베이어 라인의 시작점에서 끝점으로 이동합니다.  
아래 방향으로 보내야 하는 상자는 축소 애니메이션을 재생한 뒤 풀로 반환됩니다.

DOTween을 사용해 다음 연출을 처리합니다.

- 상자 진입 시 살짝 커졌다 돌아오는 Hop 연출
- Bomb 또는 South 방향 처리 시 축소 후 사라지는 Drop 연출
- HP 감소 및 남은 수량 UI 애니메이션

### 6. 상자 큐와 오브젝트 풀링

게임은 상자를 계속 생성하고 처리해야 하므로, 매번 새 오브젝트를 생성/삭제하지 않고 오브젝트 풀을 사용합니다.

주요 파일:

```text
Assets/Scripts/InGame/BoxPool.cs
Assets/Scripts/InGame/SouthEntryController.cs
```

`BoxPool`은 초기 상자 수를 미리 생성해 비활성화 상태로 보관하고, 필요할 때 꺼내 사용합니다.  
사용이 끝난 상자는 다시 풀에 반환됩니다.

`SouthEntryController`는 대기 슬롯 큐를 관리합니다.

흐름:

```text
시작 시 슬롯 수만큼 상자 생성
  ↓
가장 앞 상자를 분류기로 이동
  ↓
뒤 상자들을 한 칸씩 앞으로 이동
  ↓
가장 아래 슬롯에 새 상자 생성
  ↓
분류기가 비면 다시 반복
```

### 7. 게임 진행 관리

`GameController`는 제한 시간, HP, 처리 목표 수량, 클리어/실패 조건을 관리합니다.

주요 파일:

```text
Assets/Scripts/InGame/GameController.cs
```

기본 진행 규칙:

| 항목 | 내용 |
| --- | --- |
| 최대 HP | 3 |
| 오분류 페널티 | HP 1 감소 |
| 제한 시간 | 60초 |
| 목표 처리량 | 140개 |
| 클리어 조건 | 제한 시간 안에 목표 수량 처리 |
| 실패 조건 | HP 0 또는 시간 초과 |

정답 처리 시 처리 수량이 증가하고, 오답 처리 시 HP가 감소합니다.  
클리어 또는 실패 시 입력 오브젝트를 비활성화하고, 특정 UI를 점멸시킨 뒤 결과 씬으로 이동합니다.

## 프로젝트 구조

```text
Assets/
├─ Scenes/
│  └─ GameScene.unity
├─ Scripts/
│  ├─ InGame/
│  │  ├─ AdvancedGestureRecognizer.cs
│  │  ├─ GestureDirectionRecognizer.cs
│  │  ├─ DragInputController.cs
│  │  ├─ Direction8.cs
│  │  ├─ BoxPayloadType.cs
│  │  ├─ BoxController.cs
│  │  ├─ BoxPool.cs
│  │  ├─ ConveyorLane.cs
│  │  ├─ SorterController.cs
│  │  ├─ SouthEntryController.cs
│  │  └─ GameController.cs
│  └─ UI/
├─ Plugins/
│  └─ Demigiant/
├─ Settings/
└─ TextMesh Pro/
```

## 기술 스택

| 분류 | 사용 기술 |
| --- | --- |
| Engine | Unity 6 |
| Language | C# |
| Input | Unity Legacy Mouse Input |
| UI | Unity UI, TextMesh Pro |
| Tweening | DOTween Pro |
| Rendering | Universal Render Pipeline |
| Core Logic | 8방향 제스처 인식, 오브젝트 풀링, 컨베이어 라우팅 |

## 구현 의도

Boxer에서 가장 중요한 부분은 **드래그를 단순한 방향 벡터로만 처리하지 않고, 입력 궤적 자체를 분석해 안정적인 방향 판정을 만드는 것**입니다.

마우스 드래그는 플레이어마다 길이, 속도, 흔들림이 다르기 때문에 시작점과 끝점만 사용하면 애매한 입력이 생길 수 있습니다. 그래서 입력 좌표를 누적하고, 일정 개수로 리샘플링한 뒤, 정규화된 8방향 템플릿과 비교하는 방식으로 구현했습니다.

또한 최종 방향을 결정할 때 제스처 템플릿 점수만 사용하는 것이 아니라, 시작점과 끝점의 벡터 방향을 보조 점수로 사용합니다. 이 방식은 빠르게 플레이할 때 발생할 수 있는 흔들린 드래그를 어느 정도 보정하기 위한 구조입니다.

상자 처리 구조는 큐와 오브젝트 풀을 사용해 반복 생성/삭제 비용을 줄이고, 분류기가 비워질 때마다 다음 상자를 밀어 넣는 방식으로 구성했습니다. 덕분에 게임 흐름은 계속 이어지고, 플레이어는 제한 시간 동안 빠르게 판단하고 입력하는 데 집중할 수 있습니다.

## 주요 구현 포인트

- 마우스 드래그 기반 8방향 입력
- 최소 드래그 거리 기준으로 실수 입력 필터링
- 리샘플링, 정규화, 템플릿 비교 기반 제스처 인식
- 시작점-끝점 벡터 보조 점수 적용
- 상자 타입별 정답 방향 테이블 구성
- 중앙 분류기와 8방향 컨베이어 라인 연결
- 상자 큐를 통한 연속 공급 구조
- 오브젝트 풀링 기반 상자 재사용
- DOTween 기반 상자 진입/드롭/UI 피드백 연출
- HP, 제한 시간, 목표 처리량 기반 클리어/실패 조건
