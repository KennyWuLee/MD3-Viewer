using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project2
{
    enum AnimationTypes
    {
        BOTH_DEATH1 = 0,
        BOTH_DEAD1 = 1,
        BOTH_DEATH2 = 2,
        BOTH_DEAD2 = 3,
        BOTH_DEATH3 = 4,
        BOTH_DEAD3 = 5,

        TORSO_GESTURE = 6,
        TORSO_ATTACK = 7,
        TORSO_ATTACK2 = 8,
        TORSO_DROP = 9,
        TORSO_RAISE = 10,
        TORSO_STAND = 11,
        TORSO_STAND2 = 12,

        LEGS_WALKCR = 13,
        LEGS_WALK = 14,
        LEGS_RUN = 15,
        LEGS_BACK = 16,
        LEGS_SWIM = 17,
        LEGS_JUMP = 18,
        LEGS_LAND = 19,
        LEGS_JUMPB = 20,
        LEGS_LANDB = 21,
        LEGS_IDLE = 22,
        LEGS_IDLECR = 23,
        LEGS_TURN = 24,

        MAX_ANIMATIONS
    };

    public struct Animation
    {
        public int firstFrame;
        public int totalFrames;
        public int loopingFrames;
        public int fps;
    };

    //a class to represent the full model as composed of four models
    class MD3
    {
        MD3Model lowerModel;
        MD3Model upperModel;
        MD3Model headModel;
        MD3Model gunModel;
        Animation[] animations;
        int currentAnimation;

        public MD3(GraphicsDevice device, string file)
        {
            StreamReader reader = new StreamReader(File.Open(file, FileMode.Open));
            MD3Model.SetUp();
            lowerModel = new MD3Model(device);
            upperModel = new MD3Model(device);
            headModel = new MD3Model(device);
            gunModel = new MD3Model(device); //initialize new models for each of the four parts

            //read in data for each model and its skin
            lowerModel.LoadModel(reader.ReadLine());
            lowerModel.LoadSkin(reader.ReadLine());
            upperModel.LoadModel(reader.ReadLine());
            upperModel.LoadSkin(reader.ReadLine());
            headModel.LoadModel(reader.ReadLine());
            headModel.LoadSkin(reader.ReadLine());
            gunModel.LoadModel(reader.ReadLine());
            gunModel.LoadSkin(reader.ReadLine());

            //finally, load all the animations
            LoadAnimation(reader.ReadLine());
            currentAnimation = 0;
            setAnimation(); //initialize the animation

            lowerModel.Link("tag_torso", upperModel);
            upperModel.Link("tag_head", headModel);
            upperModel.Link("tag_weapon", gunModel);

            reader.Close();
            Console.Out.WriteLine();
        }

        public void Render(BasicEffect basicEffect, Matrix current, Matrix next)
        {
            lowerModel.DrawAllModels(basicEffect, current, next);
        }

        public void Update(float time)
        {
            float passedFrames = time * animations[currentAnimation].fps / 2.0f; //the number of frames we've gone through is based on the time. The division by 2 slows down the animation to a reasonable speed
            lowerModel.UpdateFrame(passedFrames); //each model updates by the number of frames passed
            upperModel.UpdateFrame(passedFrames);
            headModel.UpdateFrame(passedFrames);
            gunModel.UpdateFrame(passedFrames);
        }

        //load the animations from the config file
        public void LoadAnimation(string file)
        {
            animations = new Animation[25]; //an array to hold all 25 different animations
            StreamReader reader = new StreamReader(File.Open(file, FileMode.Open));
            Animation animation;
            string line;
            int i = 0;
            while((line = reader.ReadLine()) != null)
            {
                if(! line.Equals("") && ! line.StartsWith("//") && Char.IsDigit(line[0])) //if the line isn't blank, isn't a comment and begins with a digit
                {
                    string[] split = line.Split(null); //split on whitespace
                    animation.firstFrame = Int32.Parse(split[0]); //read in each element of the animation
                    animation.totalFrames = Int32.Parse(split[1]);
                    animation.loopingFrames = Int32.Parse(split[2]);
                    animation.fps = Int32.Parse(split[3]);
                    animations[i] = animation; //add the animation to the array of animations
                    ++i;
                }
            }
            reader.Close();
            
            //adjustment
            int skip = animations[(int)AnimationTypes.TORSO_GESTURE].firstFrame - animations[(int)AnimationTypes.LEGS_WALKCR].firstFrame;

            for(int j = (int)AnimationTypes.LEGS_WALKCR; j <= (int)AnimationTypes.LEGS_TURN; ++j)
               animations[j].firstFrame -= skip;
        }

        public void nextAnimation()
        {
            currentAnimation = (currentAnimation + 1) % 25; //cycle through each animation, starting at the beginning            
            setAnimation();
        }

        public void setAnimation()
        {
            if (currentAnimation <= (int)AnimationTypes.BOTH_DEAD3) //for the animations that affect both, set the lower and uper models
            {
                lowerModel.setAnimation(animations[currentAnimation].firstFrame, animations[currentAnimation].totalFrames);
                upperModel.setAnimation(animations[currentAnimation].firstFrame, animations[currentAnimation].totalFrames);
            }
            else if (currentAnimation <= (int)AnimationTypes.TORSO_STAND2) //for torso animations
            {
                lowerModel.setAnimation(animations[(int)AnimationTypes.LEGS_IDLE].firstFrame, animations[(int)AnimationTypes.LEGS_IDLE].totalFrames); //legs are idle
                upperModel.setAnimation(animations[currentAnimation].firstFrame, animations[currentAnimation].totalFrames);
            }
            else //if it isn't both, and it isn't a torso animation, it must be a leg animation
            {
                lowerModel.setAnimation(animations[currentAnimation].firstFrame, animations[currentAnimation].totalFrames);
                upperModel.setAnimation(animations[(int)AnimationTypes.TORSO_STAND].firstFrame, animations[(int)AnimationTypes.TORSO_STAND].totalFrames);
            }
        }
    }
}
