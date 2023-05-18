using System;
using System.Collections.Generic;

[Serializable]
public class GeneticSaveData<T>
{
    public List<T[]> PopulationGenes;
    public int Generation;

}
