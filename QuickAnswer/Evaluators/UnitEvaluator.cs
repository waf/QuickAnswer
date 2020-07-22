using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.NumberWithUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnitsNet;
using UnitsNet.Units;

namespace QuickAnswer.Evaluators
{
    class UnitEvaluator : IEvaluator
    {
        private readonly Dictionary<string, List<Enum>> units;

        public UnitEvaluator()
        {
            Type[] unitTypes = { typeof(AccelerationUnit), typeof(AmountOfSubstanceUnit), typeof(AmplitudeRatioUnit), typeof(AngleUnit), typeof(ApparentEnergyUnit), typeof(ApparentPowerUnit), typeof(AreaDensityUnit), typeof(AreaMomentOfInertiaUnit), typeof(AreaUnit), typeof(BitRateUnit), typeof(BrakeSpecificFuelConsumptionUnit), typeof(CapacitanceUnit), typeof(CoefficientOfThermalExpansionUnit), typeof(DensityUnit), typeof(DurationUnit), typeof(DynamicViscosityUnit), typeof(ElectricAdmittanceUnit), typeof(ElectricChargeDensityUnit), typeof(ElectricChargeUnit), typeof(ElectricConductanceUnit), typeof(ElectricConductivityUnit), typeof(ElectricCurrentDensityUnit), typeof(ElectricCurrentGradientUnit), typeof(ElectricCurrentUnit), typeof(ElectricFieldUnit), typeof(ElectricInductanceUnit), typeof(ElectricPotentialAcUnit), typeof(ElectricPotentialChangeRateUnit), typeof(ElectricPotentialDcUnit), typeof(ElectricPotentialUnit), typeof(ElectricResistanceUnit), typeof(ElectricResistivityUnit), typeof(ElectricSurfaceChargeDensityUnit), typeof(EnergyUnit), typeof(EntropyUnit), typeof(ForceChangeRateUnit), typeof(ForcePerLengthUnit), typeof(ForceUnit), typeof(FrequencyUnit), typeof(FuelEfficiencyUnit), typeof(HeatFluxUnit), typeof(HeatTransferCoefficientUnit), typeof(IlluminanceUnit), typeof(InformationUnit), typeof(IrradianceUnit), typeof(IrradiationUnit), typeof(KinematicViscosityUnit), typeof(LapseRateUnit), typeof(LengthUnit), typeof(LevelUnit), typeof(LinearDensityUnit), typeof(LinearPowerDensityUnit), typeof(LuminosityUnit), typeof(LuminousFluxUnit), typeof(LuminousIntensityUnit), typeof(MagneticFieldUnit), typeof(MagneticFluxUnit), typeof(MagnetizationUnit), typeof(MassConcentrationUnit), typeof(MassFlowUnit), typeof(MassFluxUnit), typeof(MassFractionUnit), typeof(MassMomentOfInertiaUnit), typeof(MassUnit), typeof(MolarEnergyUnit), typeof(MolarEntropyUnit), typeof(MolarMassUnit), typeof(MolarityUnit), typeof(PermeabilityUnit), typeof(PermittivityUnit), typeof(PowerDensityUnit), typeof(PowerRatioUnit), typeof(PowerUnit), typeof(PressureChangeRateUnit), typeof(PressureUnit), typeof(RatioChangeRateUnit), typeof(RatioUnit), typeof(ReactiveEnergyUnit), typeof(ReactivePowerUnit), typeof(RotationalAccelerationUnit), typeof(RotationalSpeedUnit), typeof(RotationalStiffnessPerLengthUnit), typeof(RotationalStiffnessUnit), typeof(SolidAngleUnit), typeof(SpecificEnergyUnit), typeof(SpecificEntropyUnit), typeof(SpecificVolumeUnit), typeof(SpecificWeightUnit), typeof(SpeedUnit), typeof(TemperatureChangeRateUnit), typeof(TemperatureDeltaUnit), typeof(TemperatureUnit), typeof(ThermalConductivityUnit), typeof(ThermalResistanceUnit), typeof(TorquePerLengthUnit), typeof(TorqueUnit), typeof(VitaminAUnit), typeof(VolumeConcentrationUnit), typeof(VolumeFlowUnit), typeof(VolumePerLengthUnit), typeof(VolumeUnit) };

            units = unitTypes
                .SelectMany(unit => Enum.GetValues(unit).Cast<Enum>())
                .Where(unit => unit.ToString() != "Undefined")
                .ToLookup(unit => Regex.Replace(unit.ToString(), "[a-z][A-Z]", m => m.Value[0] + " " + m.Value[1]))
                .ToDictionary(unit => unit.Key.ToLower(), unit => unit.ToList());

            // overrides
            units["c"] = new List<Enum>() { TemperatureUnit.DegreeCelsius };
            units["f"] = new List<Enum>() { TemperatureUnit.DegreeFahrenheit };
            units["k"] = new List<Enum>() { TemperatureUnit.Kelvin };
            units["mph"] = new List<Enum>() { SpeedUnit.MilePerHour };
            units["kph"] = new List<Enum>() { SpeedUnit.KilometerPerHour };
        }

        public Task<object> AnswerAsync(Question question)
        {
            var questionText = question.ToString();
            var results = NumberWithUnitRecognizer.RecognizeDimension(questionText, Culture.English)
                .Union(NumberWithUnitRecognizer.RecognizeTemperature(questionText, Culture.English))
                .ToList();

            // unit conversion - using microsoft recognizers for extraction
            if (results.Count == 2)
            {
                var from = results[0].AttributesStrings();
                var to = results[1].AttributesStrings();

                if (TryConvert(double.Parse(from["value"]), from["unit"], to["unit"], out var result))
                {
                    return Task.FromResult<object>(Math.Round(result, 10));
                }
            }

            // unit conversion - using regex for extraction
            var match = Regex.Match(questionText, @"(?<number>[-\.\d]+) (?<fromUnit>[\w ]*) (?:to|in|as) (?<toUnit>[\w ]*)"); // match "<Number> meters to miles"
            if(match.Success)
            {
                var number = double.Parse(match.Groups["number"].Value);
                var fromUnit = match.Groups["fromUnit"].Value;
                var toUnit = match.Groups["toUnit"].Value;

                if (TryConvert(number, fromUnit, toUnit, out var result))
                {
                    return Task.FromResult<object>(Math.Round(result, 10));
                }

            }

            throw new Exception("Not matched");
        }

        bool TryConvert(double value, string from, string to, out double result)
        {
            from = from.ToLower().TrimEnd('s');
            to = to.ToLower().TrimEnd('s');

            var fromUnits = units[from];
            var toUnits = units[to];

            foreach (var fromUnit in fromUnits)
            {
                foreach (var toUnit in toUnits)
                {
                    if(UnitConverter.TryConvert(value, fromUnit, toUnit, out result))
                    {
                        return true;
                    }
                }
            }

            result = 0;
            return false;
        }
    }
}
