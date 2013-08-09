//MDF
// 

using UnityEngine;
using System.Collections;
using System.IO;

using System.Text;
using System.Collections.Generic;

/*=======================

    Chip-8 description

    PONG OPCODES

    0x1507
    0x1777
    0x188E
    0x1AC7
    0x1E0
    0x1F8B

    0x22
    0x2A63

    0x3012
    0x3316
    0x343E
    0x368

    0x4678

    0x5574

    0x60F0
    0x6414
    0x6A02
    0x6B0C
    0x6C3F
    0x6DA2
    
    0x7640
    0x7949
    0x7D0D
    
    0x8070
    0x8247
    0x8487
    0x869
    0x8D86
    
    0x9461
    
    0xA17B
    0xAD5
    
    0xBAC8
    0xB58A
    0xB6DC
    
    0xC220

    0xD466
    0xD66E

    0xEADA
    0xEEEE

    0xF129
    0xF265
    0xFE04
    0xFF71

*/

public class Chip8
{
    /*=================
        RAM

        [0x000, 0x1FF]: Interpreter or other (e.g. font set)
        [0x050, 0x0A0]: Font set 0-F
        [0x200, 0xFFF]: addressable area for ROM and work RAM
    */
    public byte[] ram = new byte[0xFFF];

    byte[] fontset =
{ 
    0xF0, 0x90, 0x90, 0x90, 0xF0, //0
    0x20, 0x60, 0x20, 0x20, 0x70, //1
    0xF0, 0x10, 0xF0, 0x80, 0xF0, //2
    0xF0, 0x10, 0xF0, 0x10, 0xF0, //3
    0x90, 0x90, 0xF0, 0x10, 0x10, //4
    0xF0, 0x80, 0xF0, 0x10, 0xF0, //5
    0xF0, 0x80, 0xF0, 0x90, 0xF0, //6
    0xF0, 0x10, 0x20, 0x40, 0x40, //7
    0xF0, 0x90, 0xF0, 0x90, 0xF0, //8
    0xF0, 0x90, 0xF0, 0x10, 0xF0, //9
    0xF0, 0x90, 0xF0, 0x90, 0x90, //A
    0xE0, 0x90, 0xE0, 0x90, 0xE0, //B
    0xF0, 0x80, 0x80, 0x80, 0xF0, //C
    0xE0, 0x90, 0x90, 0x90, 0xE0, //D
    0xF0, 0x80, 0xF0, 0x80, 0xF0, //E
    0xF0, 0x80, 0xF0, 0x80, 0x80  //F
};

    /*=================
        Registers
        
        V0 to VE = this
        VF = carry 
        I = index
        PC = program counter
    */
    public byte[] V = new byte[0x10];
    public ushort I;
    public ushort PC;
    public ushort SP;

    /*=================
        Vanilla stack

        16 layers used to store PC. 
    */
    public ushort[] stack = new ushort[16];


    /*=================
        Timers
        
        60Hz tick
    */
    public byte delayTimer;
    public byte soundTimer;


    /*=================
        Input

    */
    public byte[] inputKeys = new byte[0x10];


    /*=================
        Graphics

        Call it VRAM but it is simply the display screen (pixels)
    */
    public byte[] vram = new byte[64 * 32];

    public bool drawRequest = true;

    //holds current 2-byte long opcode
    //public ushort opcode;

    public Chip8()
    {
        Reset();
    }

    public void Reset()
    {
        for (int i = 0; i < 0xFFF; i++)
        {
            ram[i] = 0;
        }

        for (int i = 0; i < 80; ++i)
            ram[i] = fontset[i];

        for (int i = 0; i < 0x10; i++)
        {
            V[i] = 0;
            stack[i] = 0;
            inputKeys[i] = 0;
        }

        for (int i = 0; i < 64 * 32; i++)
        {
            vram[i] = 0;
        }

        I = 0;
        PC = 0x200;
        SP = 0;

        delayTimer = 0;
        soundTimer = 0;

        //opcode = 0;
    }

    public void ComputeCycle()
    {
        //Debug.Log("ComputeCycle");

        ushort opcode = FetchOpcode();
        DecodeOpcode(opcode);
        ExecuteOpcode();
    }

    //fetch from memory
    private ushort FetchOpcode()
    {
        ushort currentPC = this.PC;
        byte op1 = this.ram[currentPC];
        byte op2 = this.ram[currentPC + 1];

        //build 2 byte opcode
        ushort opcode = (ushort)(op1 << 8);
        opcode |= op2;

        //Debug.Log("opcode = " + "0x" + opcode.ToString("X2")
        //    + " | SP:" + this.SP + "|PC:"+this.PC+"|T:"+this.delayTimer) ;

        return opcode;
    }

    //decode instruction
    /*

        Opcode	Explanation
    ------------------------------------
        0NNN	Calls RCA 1802 program at address NNN.
        00E0	Clears the screen.
        00EE	Returns from a subroutine.
        1NNN	Jumps to address NNN.
        2NNN	Calls subroutine at NNN.
        3XNN	Skips the next instruction if VX equals NN.
        4XNN	Skips the next instruction if VX doesn't equal NN.
        5XY0	Skips the next instruction if VX equals VY.
        6XNN	Sets VX to NN.
        7XNN	Adds NN to VX.
        8XY0	Sets VX to the value of VY.
        8XY1	Sets VX to VX or VY.
        8XY2	Sets VX to VX and VY.
        8XY3	Sets VX to VX xor VY.
        8XY4	Adds VY to VX. VF is set to 1 when there's a carry, and to 0 when there isn't.
        8XY5	VY is subtracted from VX. VF is set to 0 when there's a borrow, and 1 when there isn't.
        8XY6	Shifts VX right by one. VF is set to the value of the least significant bit of VX before the shift.[2]
        8XY7	Sets VX to VY minus VX. VF is set to 0 when there's a borrow, and 1 when there isn't.
        8XYE	Shifts VX left by one. VF is set to the value of the most significant bit of VX before the shift.[2]
        9XY0	Skips the next instruction if VX doesn't equal VY.
        ANNN	Sets I to the address NNN.
        BNNN	Jumps to the address NNN plus V0.
        CXNN	Sets VX to a random number and NN.
        DXYN	Draws a sprite at coordinate (VX, VY) that has a width of 8 pixels and a height of N pixels. Each row of 8 pixels is read as bit-coded (with the most significant bit of each byte displayed on the left) starting from memory location I; I value doesn't change after the execution of this instruction. As described above, VF is set to 1 if any screen pixels are flipped from set to unset when the sprite is drawn, and to 0 if that doesn't happen.
        EX9E	Skips the next instruction if the key stored in VX is pressed.
        EXA1	Skips the next instruction if the key stored in VX isn't pressed.
        FX07	Sets VX to the value of the delay timer.
        FX0A	A key press is awaited, and then stored in VX.
        FX15	Sets the delay timer to VX.
        FX18	Sets the sound timer to VX.
        FX1E	Adds VX to I.[3]
        FX29	Sets I to the location of the sprite for the character in VX. Characters 0-F (in hexadecimal) are represented by a 4x5 font.
        FX33	Stores the Binary-coded decimal representation of VX, with the most significant of three digits at the address in I, the middle digit at I plus 1, and the least significant digit at I plus 2.
        FX55	Stores V0 to VX in memory starting at address I.[4]
        FX65	Fills V0 to VX with values from memory starting at address I.[4]


        Notes

        [1] - "VIPER for RCA VIP owner". Intelligent Machines Journal (InfoWorld) (InfoWorld Media Group): pp. 9. 1978-12-11. Retrieved 2010-01-30.
        [2] - a b On the original interpreter, the value of VY is shifted, and the result is stored into VX. On current implementations, Y is ignored.
        [3] - VF is set to 1 when range overflow (I+VX>0xFFF), and 0 when there isn't. This is undocumented feature of the Chip-8 and used by Spacefight 2019! game.
        [4] - a b On the original interpreter, when the operation is done, I=I+X+1.

    */
    private void DecodeOpcode(ushort opcode)
    {
        ushort opcodePrefix = (ushort)((opcode & 0xF000) >> 12);
        ushort opcodeSuffix = (ushort)((opcode & 0x0FFF));
        ushort opcodePartX = (ushort)((opcode & 0x0F00) >> 8);
        ushort opcodePartY = (ushort)((opcode & 0x00F0) >> 4);
        ushort opcodePartZ = (ushort)((opcode & 0x000F));
        ushort opcodePartNN = (ushort)((opcode & 0x00FF));


        /*Debug.Log("opcode Prefix = " + opcodePrefix.ToString("X2"));
        Debug.Log("opcode Suffix = " + opcodeSuffix.ToString("X2"));
        Debug.Log("opcode X = " + opcodePartX.ToString("X2"));
        Debug.Log("opcode Y = " + opcodePartY.ToString("X2"));
        Debug.Log("opcode NN = " + opcodePartNN.ToString("X2"));*/

        switch (opcodePrefix)
        {
            case 0x0:
                if (opcodePartZ == 0xE)
                //(opcodeSuffix == 0x0EE)
                {
                    this.SP--;
                    this.PC = this.stack[this.SP];
                    StepCycle();
                }
                else if (opcodePartNN == 0x00 || opcodeSuffix == 0x0E0)
                {
                    for (int i = 0; i < 64 * 32; i++)
                    {
                        this.vram[i] = 0;
                    }
                    this.drawRequest = true;
                    StepCycle();
                }
                else
                {
                    Debug.LogError("Could not decode the following op: 0x" + opcode.ToString("X2"));
                    StepCycle();
                }
                break;

            case 0x1:
                this.PC = opcodeSuffix;
                break;

            case 0x2:
                //push PC to the stack
                this.stack[this.SP] = this.PC;
                this.SP++;

                //set PC
                this.PC = opcodeSuffix;
                break;

            case 0x3:
                if (this.V[opcodePartX] == opcodePartNN)
                {
                    StepCycle();
                }
                StepCycle();
                break;

            case 0x4:
                if (this.V[opcodePartX] != opcodePartNN)
                {
                    StepCycle();
                }
                StepCycle();
                break;

            case 0x5:
                if (this.V[opcodePartX] == this.V[opcodePartY])
                {
                    StepCycle();
                }
                StepCycle();
                break;

            case 0x6:
                this.V[opcodePartX] = (byte)opcodePartNN;
                StepCycle();
                break;


            case 0x7:
                this.V[opcodePartX] += (byte)opcodePartNN;
                StepCycle();
                break;

            case 0x8:
                if (opcodePartZ == 0x0)
                {
                    this.V[opcodePartX] = this.V[opcodePartY];
                    StepCycle();
                }
                else if (opcodePartZ == 0x1)
                {
                    this.V[opcodePartX] |= this.V[opcodePartY];
                    StepCycle();
                }
                else if (opcodePartZ == 0x2)
                {
                    this.V[opcodePartX] &= this.V[opcodePartY];
                    StepCycle();
                }
                else if (opcodePartZ == 0x3)
                {
                    this.V[opcodePartX] ^= this.V[opcodePartY];
                    StepCycle();
                }
                else if (opcodePartZ == 0x4)
                {
                    /*ushort sumOverflow = (ushort)((ushort)this.V[opcodePartX] + (ushort)this.V[opcodePartY]);
                    this.V[opcodePartX] = (byte)sumOverflow;
                    this.V[0xF] = (byte)((sumOverflow > 0xFF) ? 1 : 0);    //set carry flag on overflow*/

                    if (this.V[opcodePartY] > (0xFF - this.V[opcodePartX]))
                    {
                        this.V[0xF] = 1; //carry
                    }
                    else
                    {
                        this.V[0xF] = 0;
                    }

                    this.V[opcodePartX] += this.V[opcodePartY];
                    StepCycle();
                }
                else if (opcodePartZ == 0x5)
                {
                    //this.V[opcodePartX] -= this.V[opcodePartY];
                    //Debug.LogWarning("borrow is not working correctly");

                    if (this.V[opcodePartY] > this.V[opcodePartX])
                    {
                        this.V[0xF] = 0; //borrow
                    }
                    else
                    {
                        this.V[0xF] = 1;
                    }

                    this.V[opcodePartX] -= this.V[opcodePartY];
                    StepCycle();
                }
                else if (opcodePartZ == 0x6)
                {
                    //save least significant bit
                    this.V[0xF] = (byte)(this.V[opcodePartX] & 0x1);
                    this.V[opcodePartX] >>= 1;
                    StepCycle();
                }
                else if (opcodePartZ == 0x7)
                {
                    if (this.V[opcodePartX] > this.V[opcodePartY])	// VY-VX
                    {
                        this.V[0xF] = 0; //borrow
                    }
                    else
                    {
                        this.V[0xF] = 1;
                    }
                    this.V[opcodePartX] = (byte)(this.V[opcodePartY] - this.V[opcodePartX]);
                    StepCycle();
                }
                else if (opcodePartZ == 0xE)
                {
                    this.V[0xF] = (byte)(this.V[opcodePartX] >> 7);
                    this.V[opcodePartX] <<= 1;
                    StepCycle();
                }
                else
                {
                    Debug.LogError("Could not decode the following op: 0x" + opcodePrefix.ToString("X2") + opcodeSuffix.ToString("X2"));
                }
                break;

            case 0x9:
                if (this.V[opcodePartX] != this.V[opcodePartY])
                {
                    StepCycle();
                }
                StepCycle();
                break;

            case 0xA:
                this.I = opcodeSuffix;
                StepCycle();
                break;

            case 0xB:
                this.PC = (ushort)(opcodeSuffix + this.V[0]);
                break;

            case 0xC:
                this.V[opcodePartX] = (byte)((Random.Range(0, 255) % 0xFF) & opcodePartNN);
                StepCycle();
                break;

            case 0xD:
                //ushort height = opcodePartZ;
                //this.vram[opcodePartX*32 + opcodePartY] 
                ushort x = this.V[opcodePartX];
                ushort y = this.V[opcodePartY];
                ushort height = opcodePartZ;
                ushort pixel;

                this.V[0xF] = 0;

                /*for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < 8; w++)
                    {
                        this.vram[(x+w) + (y+h)] = 1;
                    }
                }*/
                for (int yline = 0; yline < height; yline++)
                {
                    pixel = this.ram[this.I + yline];
                    for (int xline = 0; xline < 8; xline++)
                    {
                        if ((pixel & (0x80 >> xline)) != 0)
                        {
                            int value = (x + xline + ((y + yline) * 64));
                            //Debug.Log("value = " + value);
                            if (value < 2048)
                            {
                                if (this.vram[value] == 1)
                                {
                                    this.V[0xF] = 1;
                                }
                                this.vram[value] ^= 1;
                            }
                        }
                    }
                }

                this.drawRequest = true;
                StepCycle();

                break;

            case 0xE:
                if (opcodePartNN == 0x9E)
                {
                    if (this.inputKeys[this.V[opcodePartX]] != 0)
                    {
                        StepCycle();
                    }
                    StepCycle();
                }
                else if (opcodePartNN == 0xA1)
                {
                    if (this.inputKeys[this.V[opcodePartX]] == 0)
                    {
                        StepCycle();
                    }
                    StepCycle();
                }
                else
                {
                    Debug.LogError("Could not decode the following op: 0x" + opcodePrefix.ToString("X2") + opcodeSuffix.ToString("X2"));
                }
                break;

            case 0xF:
                if (opcodePartNN == 0x07)
                {
                    this.V[opcodePartX] = this.delayTimer;
                    StepCycle();
                }
                else if (opcodePartNN == 0x0A)
                {
                    bool keypressed = false;

                    for (byte i = 0; i < 0xF; i++)
                    {
                        if (inputKeys[i] != 0)
                        {
                            this.V[opcodePartX] = i;
                            keypressed = true;
                        }
                    }

                    //if key was not pressed, try again next cycle
                    if (!keypressed)
                    {
                        return;
                    }

                    StepCycle();
                }
                //FX0A	A key press is awaited, and then stored in VX.
                else if (opcodePartNN == 0x15)
                {
                    this.delayTimer = this.V[opcodePartX];
                    StepCycle();
                }
                else if (opcodePartNN == 0x18)
                {
                    this.soundTimer = this.V[opcodePartX];
                    StepCycle();
                }
                else if (opcodePartNN == 0x1E)
                {
                    if (this.I + this.V[opcodePartX] > 0xFFF)
                    {
                        this.V[0xF] = 1;
                    }
                    else
                    {
                        this.V[0xF] = 0;
                    }
                    this.I += this.V[opcodePartX];
                    StepCycle();
                }
                else if (opcodePartNN == 0x29)
                {
                    this.I = (ushort)(this.V[opcodePartX] * 0x5);
                    StepCycle();
                }
                else if (opcodePartNN == 0x33)
                {
                    this.ram[this.I] = (byte)(this.V[opcodePartX] / 100);
                    this.ram[this.I + 1] = (byte)((this.V[opcodePartX] / 10) % 10);
                    this.ram[this.I + 2] = (byte)((this.V[opcodePartX] % 100) % 10);
                    StepCycle();
                }
                else if (opcodePartNN == 0x55)
                {
                    for (int i = 0; i <= opcodePartX; i++)
                    {
                        this.ram[this.I + i] = this.V[i];
                    }

                    //original Interpreter (extra) behaviour
                    this.I += (ushort)(opcodePartX + 1);

                    StepCycle();
                }
                else if (opcodePartNN == 0x65)
                {
                    for (int i = 0; i <= opcodePartX; i++)
                    {
                        this.V[i] = this.ram[this.I + i];
                    }

                    //original Interpreter (extra) behaviour
                    this.I += (ushort)(opcodePartX + 1);

                    StepCycle();
                }
                else
                {
                    Debug.LogWarning("Could not decode the following op: 0x" + opcode.ToString("X2"));
                    StepCycle();
                }
                break;

            default:
                Debug.LogWarning("Could not decode the following op: 0x" + opcodePrefix.ToString("X2") + opcodeSuffix.ToString("X2"));
                break;
        }
    }

    //execute operation
    private void ExecuteOpcode()
    {
        //StepCycle();

        if (this.delayTimer > 0)
        {
            this.delayTimer--;
        }
    }

    //returns PLAY CLIP
    public bool UpdateSoundTimer()
    {
        if (this.soundTimer > 0)
        {
            if (this.soundTimer == 1)
            {
                this.soundTimer--;
                return true;
            }
            this.soundTimer--;
        }
        return false;
    }
    private void StepCycle()
    {
        this.PC += 2; //step
    }

    public void LoadOpcodes(byte[] bytes)
    {
        //loading ROM into memory
        //Debug.Log("loading ROM into memory. Opcodes = " + bytes.Length / 2);
        for (int ramCounter = 0; ramCounter < bytes.Length; ramCounter++)
        {
            this.ram[0x200 + ramCounter] = bytes[ramCounter];
        }
    }

    /*public void PrintRAM()
    {
        foreach (byte b in this.ram)
        {
            Debug.Log(b);
        }
    }*/

    /*public void DumpROMOpcodes()
    {
        HashSet<ushort> set = new HashSet<ushort>();

        for (int counter = 0x0; counter < 0xFFF - 0x200; counter++)
        {
            set.Add(this.ram[0x200 + counter]);
        }

        string s = "";
        HashSet<ushort>.Enumerator iterator = set.GetEnumerator();
        for (int i = 0; i < set.Count; i += 2)
        {
            iterator.MoveNext();
            ushort opcode = (ushort)(iterator.Current << 8);
            iterator.MoveNext();
            opcode |= iterator.Current;
            s += "0x" + opcode.ToString("X2") + "\n";
        }

        Debug.Log(s);
    }*/
}


public class main : MonoBehaviour 
{
    public string romName = "PONG";

    private string[] romList =
    {
        "15PUZZLE",
        "BLITZ",
        "BRIX",
        "HIDDEN",
        "INVADERS",
        "MERLIN",
        "MISSILE",
        "PONG",
        "SYZYGY",
        "TANK",
        "TETRIS",
        "TICTAC",
        "UFO",
        "VBRIX",
        "VERS",
        "WIPEOFF"
    };
    private byte currentRom;

    byte[] readBuffer;

    private bool LoadROM(string romName)
    {
        //print("reading rom file...");        
        TextAsset rom = (TextAsset)Resources.Load(romName);
        byte[] romBytes = rom.bytes;
        int romSize = romBytes.Length;

        MemoryStream memoryStream = new MemoryStream(romBytes);
        BinaryReader reader = new BinaryReader(memoryStream);

        //print("File loaded. Size = " + romSize + " bytes");

        readBuffer = new byte[romSize];
        reader.Read(readBuffer, 0x0, romSize);

        return true;
    }

    private void ResetMachine()
    {
        if (LoadROM(romName))
        {
            ///////////////////

            emulator.Reset();

            //sanity check
            if (readBuffer != null)
            {
                emulator.LoadOpcodes(readBuffer);
            }
            //emulator.DumpROMOpcodes();
        }
    }

    GameObject[] screen;
    Material screenMaterialBlack;
    Material screenMaterialWhite;

    Chip8 emulator = new Chip8();
    void Start ()
	{ 
        ResetMachine();
        

        screenMaterialBlack = new Material(Shader.Find("Transparent/Diffuse"));
        screenMaterialBlack.color = new Color(0.0f, 0.0f, 0.0f, 0.15f);
        screenMaterialWhite = new Material(Shader.Find("Transparent/Diffuse"));
        screenMaterialWhite.color = Color.white;

        GameObject go = new GameObject("screen");       

        screen = new GameObject[64 * 32];
        for (int width = 0; width < 32; width++)
        {
            for (int height = 0; height < 64; height++)
            {
                int value = width*64+height;
                screen[value] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                screen[value].transform.position = new Vector3(width, height, 0) + this.transform.position;
                screen[value].transform.parent = go.transform;
                screen[value].renderer.material = screenMaterialBlack;

            }
        }


        resetRect = new Rect(screenOver2 - widthModifier, 20.0f, 100, buttonHeight);
        previousRect = new Rect(screenOver2 - widthModifier - 40, buttonHeight, buttonHeight, buttonHeight);
        nextRect = new Rect(screenOver2 + widthModifier + 10, buttonHeight, buttonHeight, buttonHeight);        
    }

    void FixedUpdate()
    {
        emulator.ComputeCycle();
        if (emulator.drawRequest)
        {
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    screen[y * 64 + x].renderer.material = emulator.vram[y * 64 + x] == 0 ? screenMaterialBlack : screenMaterialWhite;
                }
            }
            emulator.drawRequest = false;
        }

        if (emulator.UpdateSoundTimer())
        {
            this.audio.Play();
        }

        UpdateInput();
    }

    void UpdateInput() 
	{
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            emulator.inputKeys[0x1] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            emulator.inputKeys[0x1] = 0;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            emulator.inputKeys[0x2] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            emulator.inputKeys[0x2] = 0;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            emulator.inputKeys[0x3] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            emulator.inputKeys[0x3] = 0;
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            emulator.inputKeys[0xC] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.Alpha4))
        {
            emulator.inputKeys[0xC] = 0;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            emulator.inputKeys[0x4] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.Q))
        {
            emulator.inputKeys[0x4] = 0;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            emulator.inputKeys[0x5] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.W))
        {
            emulator.inputKeys[0x5] = 0;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            emulator.inputKeys[0x6] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            emulator.inputKeys[0x6] = 0;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            emulator.inputKeys[0xD] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.R))
        {
            emulator.inputKeys[0xD] = 0;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            emulator.inputKeys[0x7] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            emulator.inputKeys[0x7] = 0;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            emulator.inputKeys[0x8] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            emulator.inputKeys[0x8] = 0;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            emulator.inputKeys[0x9] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.D))
        {
            emulator.inputKeys[0x9] = 0;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            emulator.inputKeys[0xE] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.F))
        {
            emulator.inputKeys[0xE] = 0;
        }


        if (Input.GetKeyDown(KeyCode.V))
        {
            emulator.inputKeys[0xF] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.V))
        {
            emulator.inputKeys[0xF] = 0;
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            emulator.inputKeys[0xA] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.Z))
        {
            emulator.inputKeys[0xA] = 0;
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            emulator.inputKeys[0x0] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.X))
        {
            emulator.inputKeys[0x0] = 0;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            emulator.inputKeys[0xB] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.C))
        {
            emulator.inputKeys[0xB] = 0;
        }

        /*
        if (emulator.data.drawRequest)
        {
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    videoSurface.SetPixel(x, y, emulator.data.vram[y * 64 + x] == 0 ? Color.black : Color.white);
                }
            }

            videoSurface.Apply();
            emulator.data.drawRequest = false;
        }*/
	}


    float screenOver2 = Screen.width / 2;
    const float widthModifier = 50.0f;
    const float buttonHeight = 30.0f;
    Rect resetRect;
    Rect previousRect;
    Rect nextRect;

    void OnGUI()
    {
        if (GUI.Button(resetRect, romName))
        {
            ResetMachine();
        }

        if (GUI.Button(previousRect, "<"))
        {
            //previous ROM - wrap around
            currentRom--;
            if (currentRom < 0)
            {
                currentRom = 15;            
            }

            romName = romList[currentRom];
        }

        if (GUI.Button(nextRect, ">"))
        {
            //next ROM - wraps around
            currentRom++;
            if (currentRom > 15)
            {
                currentRom = 0;
            }

            romName = romList[currentRom];
        }

        /*const float buttonWidth = 40.0f;
        const float buttonHeight = 40.0f;
        if (GUI.Button(new Rect(750.0f, 50.0f, buttonWidth, buttonHeight), "1"))
        {
            emulator.inputKeys[0x1] = 1;
        }
        if (GUI.Button(new Rect(750.0f + buttonWidth, 50.0f, buttonWidth, buttonHeight), "2"))
        {
            emulator.inputKeys[0x2] = 1;
        }
        if (GUI.Button(new Rect(750.0f + buttonWidth * 2, 50.0f, buttonWidth, buttonHeight), "3"))
        {
            emulator.inputKeys[0x3] = 1;            
        }
        if(GUI.Button(new Rect(750.0f+buttonWidth*3, 50.0f, buttonWidth, buttonHeight), "C"))
        {
            emulator.inputKeys[0xC] = 1;
        }

        if (GUI.Button(new Rect(750.0f, 50.0f + buttonHeight, buttonWidth, buttonHeight), "4"))
        {
            print("button ok");
            emulator.inputKeys[0x4] = 1;
        }

        GUI.Button(new Rect(750.0f + buttonWidth, 50.0f + buttonHeight, buttonWidth, buttonHeight), "5");
        GUI.Button(new Rect(750.0f + buttonWidth*2, 50.0f + buttonHeight, buttonWidth, buttonHeight), "6");
        GUI.Button(new Rect(750.0f + buttonWidth*3, 50.0f + buttonHeight, buttonWidth, buttonHeight), "D");

        GUI.Button(new Rect(750.0f, 50.0f + buttonHeight*2, buttonWidth, buttonHeight), "7");
        GUI.Button(new Rect(750.0f + buttonWidth, 50.0f + buttonHeight * 2, buttonWidth, buttonHeight), "8");
        GUI.Button(new Rect(750.0f + buttonWidth*2, 50.0f + buttonHeight * 2, buttonWidth, buttonHeight), "9");
        GUI.Button(new Rect(750.0f + buttonWidth*3, 50.0f + buttonHeight * 2, buttonWidth, buttonHeight), "E");

        GUI.Button(new Rect(750.0f, 50.0f + buttonHeight*3, buttonWidth, buttonHeight), "A");
        GUI.Button(new Rect(750.0f + buttonWidth*1, 50.0f + buttonHeight * 3, buttonWidth, buttonHeight), "0");
        GUI.Button(new Rect(750.0f + buttonWidth*2, 50.0f + buttonHeight * 3, buttonWidth, buttonHeight), "B");
        GUI.Button(new Rect(750.0f + buttonWidth*3, 50.0f + buttonHeight * 3, buttonWidth, buttonHeight), "F");


        //GUI.Ls
        */
    }
}
