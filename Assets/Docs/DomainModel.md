# Domain Model – Úthálózati modell

Ez a dokumentum a jelenlegi map modell implementációját írja le.
Célja, hogy a csapat megértse a struktúrát, az invariánsokat és biztonságosan tudjon rá építeni (pl. generator).
---
#Újdonságok

Hozzáadva különböző segítő függvények a só és hó kezelésére

---

## Alapötlet

A map egy gráf:

* csomópont: MapNode
* út: Road
* sáv: Lane
* szegmens: LaneSegment

Egy út sávokból áll, egy sáv szegmensekből.
Az út hossza a szegmensek számából adódik.

A szerkezet létrehozás után fix.
Futás közben a LaneSegment állapot változik.

---

## Kapcsolatok

MapNode <---- Road ----> MapNode
                |
               owns
                v
              Lane
                |
               owns
                v
            LaneSegment

---

## NodeType

* Intersection
* BusStop
* Terminus

---

## MapNode

Egy csomópont a gráfban.

Tulajdonságok:

* Id
* Type
* ConnectedRoads

Szabályok:

* Id nem lehet null vagy üres
* ConnectedRoads kívülről nem módosítható
* node nem hoz létre utakat
* utak csatlakoznak hozzá

Létrehozás:

* constructor internal

---

## Road

Két node közti kapcsolat.

Tulajdonságok:

* Id
* NodeA
* NodeB
* LanesTowardsA
* LanesTowardsB
* SegmentCount

Szabályok:

* NodeA != NodeB
* egyik sem lehet null
* SegmentCount > 0
* legalább egy sáv kell
* sávok csak konstruktorban jönnek létre

Irány:

* LanesTowardsA: B -> A
* LanesTowardsB: A -> B

Létrehozás:

* constructor internal
* létrehozáskor:

  * lane-ek létrejönnek
  * node-okhoz csatlakozik

Utána:

* topology fix
* lane count fix

Segéd:

* GetAdjacentLanes(lane)

---

## Lane

Egy irányított sáv.

Tulajdonságok:

* Id
* ParentRoad
* StartNode
* EndNode
* Segments

Szabályok:

* start != end
* parentRoad nem null
* végpontok egyeznek a road végpontjaival
* segmentCount > 0

Létrehozás:

* csak Road hozza létre
* constructor internal

Utána:

* struktúra fix
* lista nem módosítható

---

## LaneSegment

A legkisebb egység.

Tulajdonságok:

* SnowLevel
* HasIce
* HasAccident

Szabályok:

* SnowLevel >= 0
* ha HasIce = true → SnowLevel = 0

Módosítás:

* AddSnow(int)
* AddSnow()
* RemoveAllSnow()
* SetIce(bool)
* SetAccident(bool)

Blocked:

* accident vagy snow >= 3

---

## Mi módosítható

* csak LaneSegment állapot

---

## Mi nem módosítható

* node id
* road végpontok
* lane struktúra
* segment lista mérete

---

## Használati példa

```csharp
var nodeA = new MapNode("A", NodeType.Intersection);
var nodeB = new MapNode("B", NodeType.BusStop);

var road = new Road(
    id: 0,
    nodeA: nodeA,
    nodeB: nodeB,
    segmentCount: 5,
    laneCountTowardsA: 1,
    laneCountTowardsB: 2
);

// lane elérés
var lane = road.LanesTowardsB[0];

// módosítás
lane[0].AddSnow();
lane[0].SetIce(true);
lane[0].RemoveAllSnow();

// lekérdezés
bool blocked = lane[0].IsBlocked();

// szomszédok
var neighbors = road.GetAdjacentLanes(lane);
```

---

## Fontos viselkedések

* GetAdjacentLanes(lane)

  * 0–2 elemet ad vissza
  * csak azonos irányban
  * exception-t dob ha lane nem ehhez az úthoz tartozik

* LaneSegment.AddSnow(int)

  * nem csinál semmit ha jég van

* LaneSegment.SetIce(true)

  * lenullázza a havat

* LaneSegment.RemoveAllSnow()

  * SnowLevel = 0

* lane sorrend index alapú (szomszédság emiatt működik)

---

## Generator szabályok

Generator csak ezt csinálja:

1. node-okat létrehoz
2. road-okat hoz létre

Paraméterek:

* start node
* end node
* segmentCount
* lanes irányonként

TILOS:

* lane-t kézzel létrehozni
* node-hoz utat kézzel adni
* listákat módosítani

---

# --- ENGLISH VERSION ---

# Domain Model – Road System

This document describes the current map model implementation.
It allows developers to safely use and extend the system (e.g. map generator).

---

## Core Concept

The map is a graph:

* Node: MapNode
* Edge: Road
* Lane: Lane
* Segment: LaneSegment

A road consists of lanes, a lane consists of segments.
Length is defined by segment count.

Structure is fixed after creation.
Only segment state changes.

---

## Relationships

MapNode <---- Road ----> MapNode
                |
               owns
                v
               Lane
                |
               owns
                v
            LaneSegment

---

## NodeType

* Intersection
* BusStop
* Terminus

---

## MapNode

Represents a graph node.

Properties:

* Id
* Type
* ConnectedRoads

Rules:

* Id must not be null or empty
* ConnectedRoads is read-only
* node does not create roads
* roads attach themselves

Creation:

* constructor is internal

---

## Road

Represents a connection between two nodes.

Properties:

* Id
* NodeA
* NodeB
* LanesTowardsA
* LanesTowardsB
* SegmentCount

Rules:

* NodeA != NodeB
* nodes must not be null
* SegmentCount > 0
* at least one lane must exist
* lanes created only in constructor

Direction:

* LanesTowardsA: B -> A
* LanesTowardsB: A -> B

Creation:

* constructor is internal
* during creation:

  * lanes are created
  * attaches to nodes

After creation:

* topology fixed
* lane count fixed

Helper:

* GetAdjacentLanes(lane)

---

## Lane

Represents a directional lane.

Properties:

* Id
* ParentRoad
* StartNode
* EndNode
* Segments

Rules:

* start != end
* endpoints must match road
* segmentCount > 0

Creation:

* created only by Road
* constructor is internal

After creation:

* structure fixed

---

## LaneSegment

Smallest unit.

Properties:

* SnowLevel
* HasIce
* HasAccident

Rules:

* SnowLevel >= 0
* if HasIce = true → SnowLevel = 0

Mutable:

* AddSnow(int)
* AddSnow()
* RemoveAllSnow()
* SetIce(bool)
* SetAccident(bool)

Blocked:

* accident OR snow >= 3

---

## What can change

* only LaneSegment state

---

## What cannot change

* node identity
* road endpoints
* lane structure
* segment count

---

## Usage Example

```csharp
var nodeA = new MapNode("A", NodeType.Intersection);
var nodeB = new MapNode("B", NodeType.BusStop);

var road = new Road(0, nodeA, nodeB, 5, 1, 2);

var lane = road.LanesTowardsB[0];

lane[0].AddSnow();
lane[0].SetIce(true);
lane[0].RemoveAllSnow();

bool blocked = lane[0].IsBlocked();

var neighbors = road.GetAdjacentLanes(lane);
```

---

## Important Behaviors

* GetAdjacentLanes(lane)

  * returns 0–2 lanes
  * same direction only
  * throws if invalid

* AddSnow does nothing if ice is present

* SetIce resets snow

* RemoveAllSnow sets SnowLevel to 0

* lane order defines adjacency

---

## Generator Rules

Generator should:

1. create nodes
2. create roads

Must NOT:

* create lanes manually
* modify node connections
* modify internal lists