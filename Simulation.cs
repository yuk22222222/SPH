using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Simulation : MonoBehaviour
{
    [SerializeField] ParticleSpawn particlespawn;
    private float mass = 1.0f;
    public float smoothingRadius = 1f;
    private float[] densityArray;
    private Vector2 [] velocityArray;
    public float gravity = 8.0f;
    public float targetDensity = 2.0f;
    public float pressureMultiplier = 50f;
    public float viscosityLength = 0.05f;
    public float collisionDumpingFactor = 0.9f;
    private List<int>[,,] gridSearch; //three-dimensional array that store the index of particle of each cell, a collection of 2d which represent each quadrant
    private int numQuadrant = 4;
   
    private float x_range = 30f;
    private float y_range = 15f;    
    void Start(){
        
        densityArray = new float [particlespawn.getNumParticle()];
        velocityArray = new Vector2 [particlespawn.getNumParticle()];
        initializeGridSearch();
        
    }


    void Update()
    {
        updateGridSearch();
        
        float particleNum = particlespawn.getNumParticle();
        //apply gravity and find density
        for (int i=0; i<particleNum;i++){
            velocityArray[i] += Vector2.down * gravity * Time.deltaTime;
            densityArray[i] = densityCalculate(particlespawn.getPosition(i));
        }

        //calcuate pressure
        for (int i=0; i<particleNum;i++){
            Vector2 pressure = pressureViscosityCalculate(i);
            Vector2 pressureAccelerate = pressure / densityArray[i]; // density should never be zero
            velocityArray[i] += pressureAccelerate * Time.deltaTime; 
        }
        //update position and move the particle by transform
        for (int i=0; i<particleNum; i++){
            collideCheck(i);
            Vector2 updatePosition = particlespawn.getPosition(i) + (velocityArray[i])*Time.deltaTime; //////
            particlespawn.setPosition(updatePosition, i);
            particlespawn.getParticleReference(i).transform.position = updatePosition;
            
        }
    }
    

    float smoothingKernel(float radius, float distance){
        //ensure only particle within radius able to exert influence
        if (distance >= radius){
            return 0;
        }
        float volume = Mathf.PI * MathF.Pow(radius,4) /6;
        //float value = Mathf.Max(0, radius*radius - distance*distance);
        return (radius - distance)*(radius-distance)/volume;
    }

    float smoothingKernelDerivative (float distance, float radius){
        if (distance>=radius){
            return 0; ////
       }
        float scale = 12/ (MathF.PI * Mathf.Pow(radius,4));
        return  (distance-radius)*scale;
    }

//derive from propertCalcualte
    public float densityCalculate (Vector2 searchPosition){
        float density = 0;
        const float mass =1;
       
        float particleNum = particlespawn.getNumParticle();
        for (int i=0; i<particleNum; i++){
            float distance = (particlespawn.getPosition(i) - searchPosition).magnitude;
            float influence = smoothingKernel(smoothingRadius, distance);
            density += mass * influence;
        }
        return density; 
    }

    // avoid repeate many times when calcuate gradient
    void storeDensity (){
        
        for(int i=0; i< densityArray.Length; i++){
            densityArray[i] = densityCalculate(particlespawn.getPosition(i));
        }
    }

    //this is reference for general property calculation, need to subsititue to exact propert
    Vector2 propertyGradient(Vector2 position){
        Vector2 propertyGradient = Vector2.zero;
        Vector2[] propertyParticles = new Vector2[particlespawn.getNumParticle ()];
        for (int i=0; i<particlespawn.getNumParticle(); i++){
            float distance = (particlespawn.getPosition(i) - position).magnitude;
            Vector2 dir = (particlespawn.getPosition(i) - position)/distance;
            float slope = smoothingKernelDerivative(distance,smoothingRadius);
            float density = densityArray[i];
            propertyGradient +=  -propertyParticles[i] * dir * slope * mass/density;
        }
        return propertyGradient;
    }
    
    float densityToPressure (float density){
        float densityError = density - targetDensity;
        float pressure = densityError * pressureMultiplier;
        return pressure;
    }

    //use to apply Newton's third law
    float sharePressureCalculate (float density1, float density2){
        float pressure1 = densityToPressure(density1);
        float pressure2 = densityToPressure(density2);
        return (pressure1+pressure2)/2;
    }

    //first calculate pressure then add the viscosity here
    public Vector2 pressureViscosityCalculate(int positionIndex){
        Vector2 searchPosition = particlespawn.getPosition(positionIndex);
        Vector2 pressureForce = Vector2.zero;
        Vector2 viscosityForce = Vector2.zero;
        // are used to find out the quadrant and specific cell index of current particle
        int quadrant = determineQuadrant(searchPosition);
        int gridXNum = gridSearch.GetLength(1);
        int gridYNum = gridSearch.GetLength(2);
        int gridX = Math.Min(gridXNum-1, (int) Math.Floor((double) searchPosition.x/smoothingRadius));
        int gridY = Math.Min(gridYNum-1, (int) Math.Floor((double) searchPosition.y/smoothingRadius));
        //assure the cell we want to seach inside the range
        int startGridX = Math.Max(0, gridX-1);
        int endGridX = Math.Min(gridXNum-1, gridX+1); //numCol-1 is max cell x index
        int startGridY = Math.Max(0, gridY-1);
        int endGridY = Math.Min(gridYNum-1,gridY+1);
        
        List<int> currentCell;
        for (int i=startGridX;i<=endGridX;i++){
            for (int j=startGridY;j<=endGridY;j++){
                currentCell = gridSearch[quadrant,i,j];
                foreach (int element in currentCell){
                    if (element == positionIndex){
                        continue;
                    }
                    float distance = (particlespawn.getPosition(element) - particlespawn.getPosition(positionIndex)).magnitude;
                    Vector2 dir;
                    if (distance==0){
                        dir = GetRandomDir();
                    }
                    else{
                        dir = (particlespawn.getPosition(element) - particlespawn.getPosition(positionIndex))/distance;
                    }
                    float slope = smoothingKernelDerivative(distance,smoothingRadius);
                    float density = densityArray[element];
                    //apply newton's thrid law
                    float sharePressure = sharePressureCalculate(density, densityArray[positionIndex]);
                    pressureForce +=  -sharePressure * dir * slope * mass/density;
                    // viscosity
                    Vector2 difVelocity = velocityArray[element] - velocityArray[positionIndex];
                    viscosityForce += slope * difVelocity;
                
                    
                }
            }
        }
        
        return pressureForce + viscosityForce * viscosityLength; 

    }

    Vector2 GetRandomDir()
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    void collideCheck (int index){
        Vector2 position = particlespawn.getPosition(index);

        if (Mathf.Abs(position.y) > y_range){
            position.y = y_range * Mathf.Sign(position.y);
            velocityArray[index].y *= -1*collisionDumpingFactor;
        }
        if (Mathf.Abs(position.x) > x_range){
            position.x = x_range * Mathf.Sign(position.x);
            velocityArray[index].x *= -1*collisionDumpingFactor;
        }
        particlespawn.setPosition(position,index);
    }

    void initializeGridSearch (){
        int gridXNum = (int) Math.Ceiling((double) x_range / smoothingRadius);
        int gridYNum = (int) Math.Ceiling((double) y_range / smoothingRadius);
        gridSearch = new List<int>[numQuadrant,gridXNum, gridYNum];
        
        for (int q = 0; q<numQuadrant; q++){
            for (int i = 0; i < gridXNum; i++)
            {
                for (int j = 0; j < gridYNum; j++)
                {
                    gridSearch [q, i, j] = new List<int>();
                }
            }
        }

    }

    void updateGridSearch (){
        int gridXNum = gridSearch.GetLength(1);
        int gridYNum = gridSearch.GetLength(2);
        //first clear all elements of every list
        for (int q=0; q<numQuadrant; q++){
            for (int i=0; i<gridXNum; i++){
                for (int j=0; j<gridYNum; j++){
                    gridSearch[q, i, j].Clear();
                }
            }
        }
        
        float x;
        float y;
        int gridX;
        int gridY;
        int quadrant;
        //loop over all particles and add them to the position required
        for (int i=0; i<particlespawn.getNumParticle(); i++){
            quadrant = determineQuadrant(particlespawn.getPosition(i));
            // each quadrant, the way to store the index inside the cell will be exactly same
            x = Math.Abs(particlespawn.getPosition(i).x);
            y = Math.Abs(particlespawn.getPosition(i).y);
            
            //this help ensure the index of cell is inside the range
            gridX = Math.Min(gridXNum-1, Math.Max((int) Math.Floor((double) x/smoothingRadius), 0)); /////////
            gridY = Math.Min(gridYNum-1, Math.Max(0, (int) Math.Floor((double) y/smoothingRadius))); ///////////
            //update
            
            gridSearch[quadrant,gridX,gridY].Add(i); ////

        } 

    }

    int determineQuadrant (Vector2 positions){
        float x = positions.x;
        float y = positions.y;
        int quadrant;

        if (x>=0 && y>=0){
            quadrant =0;
        }
        else if (x<=0 && y>=0){
            quadrant = 1;
        }
        else if (x<=0 && y<=0){
            quadrant =2;
        }
        else {
            quadrant =3;
        }
        return quadrant;
    }

}
